using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BDObjectSystem;
using Newtonsoft.Json;
using SimpleFileBrowser;
using UnityEngine;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using Animation.AnimFrame;
using GameSystem;

namespace FileSystem
{
    public class FileLoadManager : BaseManager
    {
        #region 필드 & 프로퍼티

        public BdObjectManager bdObjManager;              // BDObjectManager 참조
        public AnimObjList animObjList;                   // AnimObjList (애니메이션 관련)

        public readonly HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();
        public readonly Dictionary<string, (int, int)> FrameInfo = new Dictionary<string, (int, int)>();

        #endregion

        #region Unity 라이프사이클

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();
            SetupFileBrowser();
        }

        #endregion

        #region FileBrowser 초기 설정

        /// <summary>
        /// FileBrowser 필터 & 단축 링크 설정
        /// </summary>
        private void SetupFileBrowser()
        {
            // .bdengine, .bdstudio 확장자 필터링
            FileBrowser.SetFilters(false,
                new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

            // 런처 폴더 단축 링크
            FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");

            // OS 별 다운로드 폴더 링크
            var download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            download = Path.Combine(download, "Downloads");
            FileBrowser.AddQuickLink("Downloads", download);
        }

        #endregion

        #region 메인 임포트 (UI 호출 지점) - 파일 선택 → async 처리

        /// <summary>
        /// 여러 .bdengine/.bdstudio 파일(또는 폴더)을 임포트하는 메인 함수 (UI 버튼 등에서 호출)
        /// </summary>
        public void ImportFile()
        {
            // 파일/폴더 선택 다이얼로그 코루틴 실행 → 결과를 async 메서드로 넘김
            StartCoroutine(ShowLoadDialogCoroutine(OnFilesSelectedForMainImportAsync));
        }

        /// <summary>
        /// 파일·폴더 다이얼로그를 띄워서, 여러 개 선택 가능
        /// 선택된 경로들을 받아서 callback(Func<List<string>, Task>)으로 넘긴다.
        /// </summary>
        private IEnumerator ShowLoadDialogCoroutine(Func<List<string>, Task> callback)
        {
            yield return FileBrowser.WaitForLoadDialog(
                pickMode: FileBrowser.PickMode.FilesAndFolders,
                allowMultiSelection: true,
                initialPath: null,
                initialFilename: null,
                title: "Select Files",
                loadButtonText: "Load"
            );

            if (FileBrowser.Success)
            {
                var selectedPaths = FileBrowser.Result.ToList();
                // 여기서 바로 callback을 호출 (비동기 메서드를 코루틴 안에서 실행)
                _ = callback(selectedPaths); 
            }
            else
            {
                CustomLog.Log("파일 선택이 취소되거나 실패했습니다.");
            }
        }

        /// <summary>
        /// 다이얼로그에서 파일/폴더를 선택한 후, 실제 임포트(비동기) 로직을 수행
        /// </summary>
        private async Task OnFilesSelectedForMainImportAsync(List<string> filePaths)
        {
            // 로딩 패널 표시
            UIManger ui = GameManager.GetManager<UIManger>();
            ui.SetLoadingPanel(true);
            ui.SetLoadingText("Reading and Sorting Files...");

            try
            {
                // 메인 임포트 실행
                await ImportFilesAsync(filePaths);
            }
            catch (Exception e)
            {
                #if UNITY_EDITOR
                Debug.LogError($"임포트 중 예외 발생: {e}");
                #else
                CustomLog.LogError("불러오다가 에러러가 발생했습니다.");
                #endif
            }
            finally
            {
                ui.SetLoadingPanel(false);
            }
        }

        /// <summary>
        /// [Async] 실제 파일 목록(또는 폴더 목록)을 처리해 AnimObject 생성 & 프레임 등록
        /// </summary>
        private async Task ImportFilesAsync(List<string> filePaths)
        {
            var ui = GameManager.GetManager<UIManger>();
            var settingManager = GameManager.Setting;

            // [1] frame.txt 파싱(폴더 내에 있을 경우)
            if (settingManager.UseFrameTxtFile)
            {
                await TryParseFrameTxtAsync(filePaths, settingManager);
            }

            // [2] f<number> 패턴 정렬
            if (settingManager.UseFrameTxtFile || settingManager.UseNameInfoExtract)
            {
                filePaths = SortFiles(filePaths);
            }

            if (filePaths.Count < 1)
            {
                CustomLog.Log("임포트할 파일이 없습니다.");
                return;
            }

            // [3] 첫 번째 파일로 메인 AnimObject 생성
            ui.SetLoadingText("Making Main Display");
            string mainFileName = Path.GetFileNameWithoutExtension(filePaths[0]);
            var mainAnimObject = await MakeDisplayAsync(filePaths[0], mainFileName);

            // [4] 나머지 파일을 프레임으로 추가
            ui.SetLoadingText("Adding Frames...");
            for (int i = 1; i < filePaths.Count; i++)
            {
                var bdObject = await ProcessFileAsync(filePaths[i]);
                mainAnimObject.AddFrame(bdObject, Path.GetFileNameWithoutExtension(filePaths[i]));
            }

            // [5] HeadGenerator가 끝날 때까지 대기 (필요 시)
            ui.SetLoadingText("Waiting for Head Textures...");
            while (WorkingGenerators.Count > 0)
            {
                await Task.Delay(500);
            }

            // 마무리
            bdObjManager.EndFileImport(mainFileName);
            mainAnimObject.InitAnimModelData();
            FrameInfo.Clear();

            CustomLog.Log($"Import 완료! BDObject 개수: {bdObjManager.bdObjectCount}");
        }

        #endregion

        #region AnimObject 생성 & 파일 처리 (Async)

        /// <summary>
        /// [Async] 파일 하나를 읽어 BdObject를 씬에 추가하고, AnimObject 생성
        /// </summary>
        private async Task<AnimObject> MakeDisplayAsync(string filePath, string fileName)
        {
            var bdObject = await ProcessFileAsync(filePath);
            await bdObjManager.AddObject(bdObject);  // BdObjectManager에 등록 (코루틴이지만 Task 변환 가능 시 await)

            var animObject = animObjList.AddAnimObject(fileName);
            return animObject;
        }

        /// <summary>
        /// [Async] 파일을 읽어 BDObject로 변환(base64->gzip->JSON->BdObject)
        /// </summary>
        private async Task<BdObject> ProcessFileAsync(string filepath)
        {
            return await Task.Run(() =>
            {
                // 1) 파일에서 base64 읽기
                string base64Text = FileBrowserHelpers.ReadTextFromFile(filepath);
                var gzipData = Convert.FromBase64String(base64Text);

                // 2) gzip 해제 -> JSON
                string jsonData = DecompressGzip(gzipData);

                // 3) JSON -> BdObject
                var bdObjects = JsonConvert.DeserializeObject<BdObject[]>(jsonData);
                var bdRoot = bdObjects[0];
                BdObjectHelper.SetParent(null, bdRoot);

                return bdRoot;
            });
        }

        #endregion

        #region 단일 프레임 임포트 (기존 AnimObject에 추가)

        /// <summary>
        /// 이미 존재하는 AnimObject에 프레임 하나를 추가하는 UI 버튼용 함수
        /// </summary>
        public void ImportFrame(AnimObject target, int tick)
        {
            // 파일 선택 후, 선택된 파일을 async 메서드로 임포트
            StartCoroutine(ShowLoadDialogCoroutineForFrame(path =>
                OnFrameFileSelectedAsync(path, target, tick)
            ));
        }

        /// <summary>
        /// 코루틴으로 단일 파일만 선택 (PickMode.Files, allowMultiSelection: false)
        /// 선택된 파일 경로를 Func<string, Task> 형태 콜백으로 넘김
        /// </summary>
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
                CustomLog.Log("프레임 임포트용 파일 선택 실패 또는 취소");
            }
        }

        /// <summary>
        /// [Async] 단일 프레임 임포트: 파일을 읽어 BDObject 생성 → AnimObject.AddFrame
        /// </summary>
        private async Task OnFrameFileSelectedAsync(string filepath, AnimObject target, int tick)
        {
            var ui = GameManager.GetManager<UIManger>();
            ui.SetLoadingPanel(true);

            try
            {
                var bdObject = await ProcessFileAsync(filepath);
                target.AddFrame(
                    Path.GetFileNameWithoutExtension(filepath),
                    bdObject,
                    tick,
                    GameManager.Setting.defaultInterpolation
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"프레임 임포트 중 오류: {e}");
            }
            finally
            {
                ui.SetLoadingPanel(false);
            }
        }

        #endregion

        #region frame.txt 파싱

        /// <summary>
        /// [Async] 폴더 목록에서 frame.txt를 찾아서 FrameInfo에 등록
        /// </summary>
        private async Task TryParseFrameTxtAsync(List<string> paths, SettingManager settingManager)
        {
            FrameInfo.Clear();

            // Task.Run으로 I/O 처리(파일 읽기)를 백그라운드로 돌릴 수 있음
            await Task.Run(() =>
            {
                foreach (var p in paths)
                {
                    if (Directory.Exists(p))
                    {
                        var frameFile = Directory.GetFiles(p, "frame.txt", SearchOption.TopDirectoryOnly)
                                                 .FirstOrDefault();
                        if (!string.IsNullOrEmpty(frameFile))
                        {
                            SetDictByFrameTxt(settingManager, frameFile);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// frame.txt 한 파일을 파싱하여 FrameInfo에 (tickInterval, interpolation) 정보를 담는다
        /// </summary>
        private void SetDictByFrameTxt(SettingManager settingManager, string frameFile)
        {
            CustomLog.Log("Frame.txt Detected : " + frameFile);
            var lines = File.ReadLines(frameFile);

            foreach (var line in lines)
            {
                var parts = line.Split(' ');
                string frameKey = null;
                var sValue = settingManager.defaultTickInterval;
                var iValue = settingManager.defaultInterpolation;

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();

                    if (trimmed.StartsWith("f"))
                    {
                        frameKey = trimmed;
                    }
                    else if (trimmed.StartsWith("s"))
                    {
                        if (int.TryParse(trimmed[1..], out var s)) sValue = s;
                    }
                    else if (trimmed.StartsWith("i"))
                    {
                        if (int.TryParse(trimmed[1..], out var inter)) iValue = inter;
                    }
                }

                if (!string.IsNullOrEmpty(frameKey))
                {
                    FrameInfo[frameKey] = (sValue, iValue);
                }
            }
        }

        #endregion

        #region 파일 이름 정렬 & GZip 유틸

        /// <summary>
        /// “f<number>” 패턴 (예: f1, f2, f10...)을 기준으로 정렬
        /// 매칭 안 되는 파일은 뒤쪽에 그대로 붙임
        /// </summary>
        private List<string> SortFiles(IEnumerable<string> fileNames)
        {
            var regex = new Regex(@"f(\d+)", RegexOptions.IgnoreCase);
            var matched = new List<(string path, int number)>();
            var unmatched = new List<string>();

            foreach (var path in fileNames)
            {
                var match = regex.Match(path);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
                    matched.Add((path, num));
                else
                    unmatched.Add(path);
            }

            matched.Sort((a, b) => a.number.CompareTo(b.number));
            var sorted = matched.Select(x => x.path).ToList();
            sorted.AddRange(unmatched);

            return sorted;
        }

        /// <summary>
        /// GZip 바이트 배열 → 문자열(JSON)으로 변환
        /// </summary>
        private string DecompressGzip(byte[] gzipData)
        {
            using var compressedStream = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        #endregion
    }
}
