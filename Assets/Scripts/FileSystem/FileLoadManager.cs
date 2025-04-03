using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Display;
using FileSystem.Helpers;  // <-- 헬퍼 네임스페이스
using GameSystem;
using SimpleFileBrowser;
using UnityEngine;

namespace FileSystem
{
    public class FileLoadManager : BaseManager
    {
        #region 필드 & 프로퍼티

        public BdObjectManager bdObjManager;    // BDObjectManager 참조
        public AnimObjList animObjList;         // AnimObjList (애니메이션 관련)

        /// <summary>
        /// HeadGenerator 동작이 끝날 때까지 대기할 때 활용
        /// </summary>
        public readonly HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

        /// <summary>
        /// frame.txt에서 읽은 정보 (f키 → (tick, interpolation))
        /// </summary>
        public readonly Dictionary<string, (int, int)> FrameInfo = new Dictionary<string, (int, int)>();

        private FileBrowser.Filter loadFilter = new FileBrowser.Filter("Files", ".bdengine", ".bdstudio");

        #endregion

        #region Unity 라이프사이클

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();
            SetupFileBrowser();
        }

        #endregion

        #region FileBrowser 초기 설정

        private void SetupFileBrowser()
        {
            FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");

            // OS 별로 “Downloads” 폴더 찾기
            var download = System.IO.Path.GetDirectoryName(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            );
            download = System.IO.Path.Combine(download, "Downloads");
            FileBrowser.AddQuickLink("Downloads", download);
        }

        #endregion

        #region 메인 임포트 (UI 호출) - 파일/폴더 선택 후 async

        /// <summary>
        /// 여러 .bdengine(또는 폴더)을 임포트하는 UI 버튼. 
        /// 파일/폴더 선택 코루틴 → 비동기 임포트
        /// </summary>
        public void ImportFile()
        {
            StartCoroutine(ShowLoadDialogCoroutine(OnFilesSelectedForMainImportAsync));
        }

        /// <summary>
        /// FileBrowser로부터 여러 파일/폴더 선택
        /// </summary>
        private IEnumerator ShowLoadDialogCoroutine(Func<List<string>, Task> callback)
        {
            FileBrowser.SetFilters(false, loadFilter);

            yield return FileBrowser.WaitForLoadDialog(
                pickMode: FileBrowser.PickMode.FilesAndFolders,
                allowMultiSelection: true,
                title: "Select Files",
                loadButtonText: "Load"
            );

            if (FileBrowser.Success)
            {
                var selectedPaths = FileBrowser.Result.ToList();
                // 코루틴에서 async 메서드 호출
                _ = callback(selectedPaths);
            }
            else
            {
                CustomLog.Log("파일 선택 취소/실패");
            }
        }

        /// <summary>
        /// 선택된 파일(또는 폴더)들을 실제 임포트
        /// </summary>
        private async Task OnFilesSelectedForMainImportAsync(List<string> filePaths)
        {
            var ui = GameManager.GetManager<UIManager>();
            ui.SetLoadingPanel(true);
            ui.SetLoadingText("Reading and Sorting Files...");

            try
            {
                await ImportFilesAsync(filePaths);
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"임포트 중 예외 발생: {e}");
#else
                CustomLog.LogError("불러오던 중 에러가 발생했습니다.");
#endif
            }
            finally
            {
                ui.SetLoadingPanel(false);
            }
        }

        #endregion

        #region 메인 임포트 로직 (비동기)

        private async Task ImportFilesAsync(List<string> filePaths)
        {
            var ui = GameManager.GetManager<UIManager>();
            var settingManager = GameManager.Setting;

            // 1) frame.txt 파싱
            if (settingManager.UseFrameTxtFile)
            {
                // FrameDataHelper에 위임
                await FrameDataHelper.TryParseFrameTxtAsync(filePaths, FrameInfo, settingManager);
            }

            FileProcessingHelper.GetAllFileFromFolder(ref filePaths);

            // 2) f<number> 정렬
            if (settingManager.UseFrameTxtFile || settingManager.UseNameInfoExtract)
            {
                filePaths = FileProcessingHelper.SortFiles(filePaths);
            }

            if (filePaths.Count < 1)
            {
                CustomLog.Log("임포트할 파일이 없습니다.");
                return;
            }

            // 3) 첫 파일로 메인 디스플레이 생성
            ui.SetLoadingText("Making Main Display...");
            string mainName = System.IO.Path.GetFileNameWithoutExtension(filePaths[0]);
            var mainAnimObject = await MakeDisplayAsync(filePaths[0], mainName);

            // 4) 나머지 파일 프레임 추가
            ui.SetLoadingText("Adding Frames...");
            for (int i = 1; i < filePaths.Count; i++)
            {
                var bdObj = await FileProcessingHelper.ProcessFileAsync(filePaths[i]);
                mainAnimObject.AddFrame(bdObj, System.IO.Path.GetFileNameWithoutExtension(filePaths[i]));
            }

            // 5) HeadGenerator 대기
            ui.SetLoadingText("Waiting Head Textures...");
            while (WorkingGenerators.Count > 0)
            {
                await Task.Delay(500);
            }

            FrameInfo.Clear();

            CustomLog.Log($"Import 완료! BDObject 개수: {bdObjManager.bdObjectCount}");
        }

        /// <summary>
        /// 파일 하나로 AnimObject 생성 (BdObjectManager에 등록 후 AnimObjList에 생성)
        /// </summary>
        private async Task<AnimObject> MakeDisplayAsync(string filePath, string fileName)
        {
            // 파일 → BDObject
            var bdObject = await FileProcessingHelper.ProcessFileAsync(filePath);

            // BdObjectManager 등록
            await bdObjManager.AddObject(bdObject, fileName);

            return animObjList.AddAnimObject(fileName);
        }

        #endregion

        #region 단일 프레임 임포트

        /// <summary>
        /// 기존 AnimObject에 프레임 하나 추가 (UI 버튼)
        /// </summary>
        public void ImportFrame(AnimObject target, int tick)
        {
            StartCoroutine(ShowLoadDialogCoroutineForFrame(path =>
                OnFrameFileSelectedAsync(path, target, tick)
            ));
        }

        private IEnumerator ShowLoadDialogCoroutineForFrame(Func<string, Task> callback)
        {
            yield return FileBrowser.WaitForLoadDialog(
                pickMode: FileBrowser.PickMode.Files,
                allowMultiSelection: false
            );

            if (FileBrowser.Success)
            {
                var filePath = FileBrowser.Result[0];
                _ = callback(filePath);
            }
            else
            {
                CustomLog.Log("프레임 추가용 파일 선택 취소/실패");
            }
        }

        private async Task OnFrameFileSelectedAsync(string filePath, AnimObject target, int tick)
        {
            var ui = GameManager.GetManager<UIManager>();
            ui.SetLoadingPanel(true);

            try
            {
                var bdObject = await FileProcessingHelper.ProcessFileAsync(filePath);
                target.AddFrame(
                    System.IO.Path.GetFileNameWithoutExtension(filePath),
                    bdObject,
                    tick,
                    GameManager.Setting.defaultInterpolation
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"프레임 임포트 오류: {e}");
            }
            finally
            {
                ui.SetLoadingPanel(false);
            }
        }

        #endregion
    }
}
