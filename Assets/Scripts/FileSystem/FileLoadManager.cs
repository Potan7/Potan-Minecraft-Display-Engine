using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Animation;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using Cysharp.Threading.Tasks;
using GameSystem;
using UnityEngine;
using SFB;

namespace FileSystem
{
    public class FileLoadManager : BaseManager
    {
        #region 필드 & 프로퍼티
        public static string[] FileExtensions = { "bdengine", "bdstudio" };
        public static readonly ExtensionFilter[] extension = new[] { new ExtensionFilter("BDEngine Files", FileExtensions) };

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



        public TagUUIDAdder tagUUIDAdder;

        private UIManager uiManager;

        #endregion

        #region Unity 라이프사이클

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();
            uiManager = GameManager.GetManager<UIManager>();
        }

        #endregion

        #region 메인 임포트 (UI 호출) - 파일/폴더 선택 후 async

        /// <summary>
        /// 여러 .bdengine(또는 폴더)을 임포트하는 UI 버튼. 
        /// 파일/폴더 선택 코루틴 → 비동기 임포트
        /// </summary>
        public void ImportFile() => ShowLoadDialogCoroutine().Forget();

        /// <summary>
        /// FileBrowser로부터 여러 파일/폴더 선택
        /// </summary>
        private async UniTaskVoid ShowLoadDialogCoroutine()
        {
            // FileBrowser.SetFilters(false, loadFilter);

            // await FileBrowser.WaitForLoadDialog(
            //     pickMode: FileBrowser.PickMode.FilesAndFolders,
            //     allowMultiSelection: true,
            //     title: "Select Files",
            //     loadButtonText: "Load"
            // ).ToUniTask();

            var paths = StandaloneFileBrowser.OpenFilePanel(
                "Select Files",
                null,
                extension,
                true
            );

            if (paths.Length > 0)
            {
                // 코루틴에서 async 메서드 호출
                uiManager.SetLoadingPanel(true);
                uiManager.SetLoadingText("Reading and Sorting Files...");

                try
                {
                    await ImportFilesAsync(new List<string>(paths));
                }
                catch (Exception e)
                {
                    CustomLog.LogError($"파일 임포트 중 오류 발생: {e}");
                }
                finally
                {
                    uiManager.SetLoadingPanel(false);
                }
            }
            else
            {
                CustomLog.Log("파일 선택 취소/실패");
            }
        }

        #endregion

        #region 메인 임포트 로직

        private async UniTask ImportFilesAsync(List<string> filePaths)
        {
            var settingManager = GameManager.GetManager<SettingManager>();

            // foreach (var path in filePaths)
            // {
            //     CustomLog.Log($"선택된 파일/폴더: {path}");
            // }

            // 1) frame.txt 파싱
            // if (settingManager.UseFxSort)
            // {
            //     await TryParseFrameTxtAsync(filePaths, FrameInfo, settingManager);
            // }

            // GetAllFileFromFolder를 호출하고 결과를 다시 filePaths에 할당
            filePaths = FileProcessingHelper.GetAllFileFromFolder(filePaths);

            // 2) f<number> 정렬
            if (settingManager.UseFxSort)
            {
                filePaths = FileProcessingHelper.SortFiles(filePaths);
            }

            if (filePaths.Count < 1)
            {
                CustomLog.Log("임포트할 파일이 없습니다.");
                return;
            }

            // 3) 첫 파일로 메인 디스플레이 생성
            uiManager.SetLoadingText("Making Main Display...");
            string mainName = Path.GetFileNameWithoutExtension(filePaths[0]);
            // var mainAnimObject = await MakeDisplayAsync(filePaths[0], mainName);

            // BDObject 읽은 뒤 BDObjectManager에 추가, AnimObject 추가 
            var mainBdObject = await FileProcessingHelper.ProcessFileAsync(filePaths[0], true);

            bool isCorrectTag = BdObjectHelper.HasVaildID(mainBdObject);
            if (!isCorrectTag)
            {
                var tagObject = await AskAndApplyTagUUIDAdder(filePaths[0]);

                if (tagObject != null)
                {
                    mainBdObject = tagObject;
                }

            }

            await bdObjManager.AddObject(mainBdObject, mainName);
            var mainAnimObject = animObjList.AddAnimObject(mainName);

            // 4) 나머지 파일 프레임 추가
            uiManager.SetLoadingText("Adding Frames...");
            for (int i = 1; i < filePaths.Count; i++)
            {
                var bdObj = await FileProcessingHelper.ProcessFileAsync(filePaths[i]);

                mainAnimObject.AddFrame(bdObj, Path.GetFileNameWithoutExtension(filePaths[i]));
            }

            // 5) HeadGenerator 대기
            uiManager.SetLoadingText("Waiting Head Textures...");
            try
            {
                await UniTask.WaitWhile(() => WorkingGenerators.Count > 0).Timeout(TimeSpan.FromSeconds(100));
            }
            catch (TimeoutException)
            {
                CustomLog.LogError("머리 텍스쳐 로딩 타임아웃");
            }

            mainAnimObject.UpdateAllFrameInterJump();

            FrameInfo.Clear();

            CustomLog.Log($"Import 완료! BDObject 개수: {bdObjManager.bdObjectCount}");
        }

        // public async UniTask TryParseFrameTxtAsync(
        //     List<string> paths,
        //     Dictionary<string, (int, int)> frameInfo,
        //     SettingManager settingManager
        // )
        // {
        //     frameInfo.Clear();

        //     for (int i = 0; i < paths.Count; i++)
        //     {
        //         if (Directory.Exists(paths[i]))
        //         {
        //             var frameFile = Directory.GetFiles(paths[i], "frame.txt").FirstOrDefault();

        //             if (!string.IsNullOrEmpty(frameFile))
        //             {
        //                 // 1. 로그는 메인 스레드에서 출력
        //                 CustomLog.Log("Frame.txt Detected : " + frameFile);

        //                 // 2. 파싱은 백그라운드로
        //                 await UniTask.RunOnThreadPool(() =>
        //                 {
        //                     ParseFrameFile(frameFile, frameInfo, settingManager);
        //                 });
        //                 return;
        //             }
        //         }
        //     }
        // }

        // private static void ParseFrameFile(
        //     string frameFile,
        //     Dictionary<string, (int, int)> frameInfo,
        //     SettingManager settingManager
        //     )
        // {
        //     var lines = File.ReadLines(frameFile);

        //     foreach (var line in lines)
        //     {
        //         var parts = line.Split(' ');

        //         string frameKey = null;
        //         int sValue = settingManager.defaultTickInterval;
        //         int iValue = settingManager.defaultInterpolation;

        //         foreach (var part in parts)
        //         {
        //             var trimmed = part.Trim();

        //             if (trimmed.StartsWith("f"))
        //             {
        //                 frameKey = trimmed;
        //             }
        //             else if (trimmed.StartsWith("s") &&
        //                      int.TryParse(trimmed.Substring(1), out int s))
        //             {
        //                 sValue = s;
        //             }
        //             else if (trimmed.StartsWith("i") &&
        //                      int.TryParse(trimmed.Substring(1), out int inter))
        //             {
        //                 iValue = inter;
        //             }
        //         }

        //         if (!string.IsNullOrEmpty(frameKey))
        //         {
        //             frameInfo[frameKey] = (sValue, iValue);
        //         }
        //     }
        // }

        #endregion

        #region 단일 프레임 임포트

        /// <summary>
        /// 기존 AnimObject에 프레임 하나 추가 (UI 버튼)
        /// </summary>
        public void ImportFrame(AnimObject target, int tick)
        {
            ShowLoadDialogForSingleFrame(target, tick).Forget();
        }

        /// <summary>
        /// 단일 프레임 추가를 위해 파일 브라우저를 엽니다.
        /// </summary>
        private async UniTaskVoid ShowLoadDialogForSingleFrame(AnimObject target, int tick)
        {
            var paths = StandaloneFileBrowser.OpenFilePanel(
                "Select Frame File",
                null,
                extension,
                false
            );

            if (paths.Length > 0)
            {
                var filePath = paths[0];
                var ui = GameManager.GetManager<UIManager>();
                ui.SetLoadingPanel(true);

                try
                {
                    // 실제 임포트 로직 호출
                    await ImportSingleFrameAsync(target, filePath, tick);
                }
                catch (Exception e)
                {
                    CustomLog.LogError($"프레임 임포트 중 오류 발생: {e}");
                }
                finally
                {
                    ui.SetLoadingPanel(false);
                }
            }
            else
            {
                CustomLog.Log("프레임 추가용 파일 선택이 취소되었습니다.");
            }
        }

        /// <summary>
        /// 단일 파일을 읽어 AnimObject에 프레임으로 추가하는 로직
        /// </summary>
        private async UniTask ImportSingleFrameAsync(AnimObject target, string filePath, int tick, bool useDefaultTickInterval = false)
        {
            var bdObject = await FileProcessingHelper.ProcessFileAsync(filePath);

            if (bdObject == null)
            {
                CustomLog.Log("올바르지 않은 파일입니다. 프레임 임포트를 중단합니다.");
                return;
            }
            
            // 태그 유효성 검사
            // bool isCorrectTag = BdObjectHelper.HasVaildID(bdObject);
            // if (!isCorrectTag)
            // {
            //     bdObject = await AskAndApplyTagUUIDAdder(filePath);

            //     if (bdObject == null)
            //     {
            //         CustomLog.Log("태그 추가가 취소되어 프레임 임포트를 중단합니다.");
            //         return;
            //     }
            // }

            target.AddFrame(
                Path.GetFileNameWithoutExtension(filePath),
                bdObject,
                tick,
                GameManager.GetManager<SettingManager>().defaultInterpolation,
                useDefaultTickInterval
            );

            CustomLog.Log($"프레임 '{Path.GetFileName(filePath)}'이(가) {tick}틱에 추가되었습니다.");
        }

        #endregion

        #region 태그/UUID 추가

        async UniTask<BdObject> AskAndApplyTagUUIDAdder(string path)
        {
            CursorManager.SetCursor(CursorManager.CursorType.Default);
            // 1) CompletionSource 생성
            var tcs = new UniTaskCompletionSource<BdObject>();

            // 2) 이벤트 핸들러 정의 (로컬 함수로 캡쳐)
            void Handler(BdObject obj)
            {
                // 결과 세팅
                tcs.TrySetResult(obj);
                // 메모리 누수 방지를 위해 구독 해제
                tagUUIDAdder.OnBDObjectEdited -= Handler;
            }

            // 3) 편집 완료 이벤트 구독
            tagUUIDAdder.OnBDObjectEdited += Handler;

            // 4) 패널 띄우기
            tagUUIDAdder.SetFilePath(path);
            tagUUIDAdder.SetPanelActive(true);

            async UniTaskVoid UserCancelDetection()
            {
                await UniTask.WaitUntil(() => tagUUIDAdder.tagAdderPanel.gameObject.activeSelf == false);

                tcs.TrySetCanceled();
                tagUUIDAdder.OnBDObjectEdited -= Handler;
            }

            UserCancelDetection().Forget();

            // 5) 편집 완료 이벤트가 발생할 때까지 대기
            try
            {
                BdObject editedObject = await tcs.Task;
                tagUUIDAdder.SetPanelActive(false);
                CursorManager.SetCursor(CursorManager.CursorType.Loading);
                return editedObject;
            }
            catch (OperationCanceledException)
            {
                tagUUIDAdder.SetPanelActive(false);
                return null;
            }
        }

        #endregion

        #region 파일 드랍 & 붙여넣기

        public async void FileDroped(List<string> paths)
        {
            var ui = GameManager.GetManager<UIManager>();
            ui.SetLoadingPanel(true);
            try
            {
                // 1. 트랙이 없는 경우: 새로운 트랙으로 전체 임포트
                if (animObjList.animObjects.Count == 0)
                {
                    CustomLog.Log("애니메이션 트랙이 없습니다. 파일을 임포트합니다.");
                    // 전처리 없이 ImportFilesAsync에 위임. ImportFilesAsync가 폴더 처리를 담당.
                    await ImportFilesAsync(paths);
                    return;
                }


                // 2. 선택된 트랙이 없는 경우: 사용자에게 확인 후 새로운 트랙으로 전체 임포트
                if (animObjList.selectedAnimObject == null)
                {
                    bool isConfirmed = await ui.ShowPopupPanelAsync("새로운 트랙을 추가하시겠습니까?", "선택된 트랙이 없습니다.");
                    if (isConfirmed)
                    {
                        CustomLog.Log("새로운 트랙에 파일을 추가합니다.");
                        // 전처리 없이 ImportFilesAsync에 위임. ImportFilesAsync가 폴더 처리를 담당.
                        await ImportFilesAsync(paths);
                    }
                    return;
                }

                // 3. 선택된 트랙이 있는 경우: 사용자에게 확인 후 프레임 추가
                // 이 경우에는 폴더를 파일로 변환하는 전처리가 필요함.
                paths = FileProcessingHelper.GetAllFileFromFolder(paths);
                if (paths.Count == 0) return; // 처리할 파일이 없음

                bool isConfirmedAdd = await ui.ShowPopupPanelAsync("선택된 트랙에 파일을 추가하시겠습니까?", "해당 틱에 파일을 추가합니다.");
                if (isConfirmedAdd)
                {
                    CustomLog.Log("선택된 트랙에 파일을 추가합니다.");
                    var currentTick = GameManager.GetManager<AnimManager>().Tick;
                    foreach (var path in paths)
                    {
                        await ImportSingleFrameAsync(
                            animObjList.selectedAnimObject,
                            path,
                            Mathf.RoundToInt(currentTick),
                            true
                        );
                    }
                }
            }
            catch (Exception e)
            {
                CustomLog.LogError($"파일 드랍 처리 중 오류 발생: {e}");
            }
            finally
            {
                ui.SetLoadingPanel(false);
            }
        }

        #endregion
    }
}
