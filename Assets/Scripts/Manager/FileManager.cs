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

namespace Manager
{
    public class FileManager : BaseManager
    {
        public BdObjectManager bdObjManager;
        public AnimObjList animObjList;
        public readonly HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

        public readonly Dictionary<string, (int, int)> FrameInfo = new Dictionary<string, (int, int)>();

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();

            // filtering .bdengine, .bdstudio
            FileBrowser.SetFilters(false,
                new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

            // add launcher folder to shortcut
#if UNITY_EDITOR
            FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath);
#else
        FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");
#endif 
            
            // add download folder to shortcut
            var download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            // ReSharper disable once AssignNullToNotNullAttribute
            download = Path.Combine(download, "Downloads");

            FileBrowser.AddQuickLink("Downloads", download);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator ShowLoadDialogCoroutine(Action<List<string>> callback)
        {
            // Get file path
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Select Files", "Load");

            // if not success, result is null
            if (FileBrowser.Success)
            { 
                // get files
                var files = new List<string>();
                var result = FileBrowser.Result;

                foreach (var t in result)
                {
                    // if t is a folder
                    if (Directory.Exists(t))
                    {
                        // load all bdengine files in the folder
                        var folderFiles = Directory.GetFiles(t, "*.bdengine", SearchOption.TopDirectoryOnly);

                        var settingManager = GameManager.GetManager<SettingManager>();

                        // if using frame.txt
                        if (settingManager.UseFrameTxtFile)
                        {
                            var frameFile = Directory.GetFiles(t, "frame.txt", SearchOption.TopDirectoryOnly).FirstOrDefault();
                            //Debug.Log("Try find Frame : " + frameFile);

                            if (!string.IsNullOrEmpty(frameFile))
                                SetDictByFrameTxt(settingManager, frameFile);
                        }


                        files.AddRange(folderFiles);
                    }
                    else
                    {
                        // if t is a file
                        files.Add(t);
                    }
                }
                // if no file selected
                if (files.Count < 1)
                {
                    CustomLog.Log("No file selected");
                    yield break;
                }

                callback?.Invoke(files); 
            }
            else
            {
                CustomLog.Log("Failed to load file");
            }
        
        }

        // Frame.txt를 사용해 tick과 interpolation 설정하기
        private void SetDictByFrameTxt(SettingManager settingManager, string frameFile)
        {
            CustomLog.Log("Frame.txt Detected : " + frameFile);
            FrameInfo.Clear();
            
            var lines = File.ReadLines(frameFile);

            foreach (var line in lines)
            {
                //Debug.Log("Line : " + line);
                var parts = line.Split(' ');

                string frameKey = null;
                var sValue = settingManager.defaultTickInterval; // �⺻�� (�ʿ��ϸ� ����)
                var iValue = settingManager.defaultInterpolation; // �⺻�� (�ʿ��ϸ� ����)

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();

                    if (trimmed.StartsWith("f"))
                    {
                        frameKey = trimmed;
                    }
                    else if (trimmed.StartsWith("s"))
                    {
                        if (int.TryParse(trimmed[1..], out var s))
                            sValue = s;
                    }
                    else if (trimmed.StartsWith("i"))
                    {
                        if (int.TryParse(trimmed[1..], out var inter))
                            iValue = inter;
                    }
                }

                if (!string.IsNullOrEmpty(frameKey))
                {
                    FrameInfo[frameKey] = (sValue, iValue);
                    //Debug.Log("Frame Info : " + frameKey + " " + sValue + " " + iValue);
                }
            }
        }

        // file import
        public void ImportFile() => StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));

        // frame import
        public void ImportFrame(AnimObject target, int tick) => StartCoroutine(FrameImportCoroutine(target, tick));

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator FrameImportCoroutine(AnimObject target, int tick)
        {
            
            // Get file path
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files);

            if (FileBrowser.Success)
            {
                var filepath = FileBrowser.Result[0];
                AfterLoadFrame(filepath, target, tick);
            }
            else
            {
                CustomLog.Log("Failed to load file");
            }
        }

        // make display and frames from files
        // ReSharper disable once AsyncVoidMethod
        private async void AfterLoadFile(List<string> filePaths)
        {
            GameManager.GetManager<UIManger>().SetLoadingPanel(true);

            var settingManager = GameManager.Setting;
            if (settingManager.UseFrameTxtFile || settingManager.UseNameInfoExtract)
            {
                // sort files by f<number>
                filePaths = SortFiles(filePaths);
            }

            string fileName = Path.GetFileNameWithoutExtension(filePaths[0]);
            // generate display using first file
            var animObject = await MakeDisplay(filePaths[0], fileName);

            // reading files and adding frames
            for (var i = 1; i < filePaths.Count; i++)
            {
                var bdObject = await ProcessFileAsync(filePaths[i]);
                animObject.AddFrame(bdObject, Path.GetFileNameWithoutExtension(filePaths[i]));
            }
            // wait until all generators are done
            while (WorkingGenerators.Count > 0) await Task.Delay(500);

            GameManager.GetManager<UIManger>().SetLoadingPanel(false);
            bdObjManager.EndFileImport(fileName);
            animObject.InitAnimModelData();

            CustomLog.Log($"Import is Done! BDObject Count: {GameManager.GetManager<BdObjectManager>().bdObjectCount}");
        }

        // make frame from file
        // ReSharper disable Unity.PerformanceAnalysis
        // ReSharper disable once AsyncVoidMethod
        private async void AfterLoadFrame(string filepath, AnimObject target, int tick)
        {
            GameManager.GetManager<UIManger>().SetLoadingPanel(true);

            var bdObject = await ProcessFileAsync(filepath);
            target.AddFrame(Path.GetFileNameWithoutExtension(filepath), bdObject, tick, GameManager.Setting.defaultInterpolation);

            GameManager.GetManager<UIManger>().SetLoadingPanel(false);
        }

        // get bdObjects from file
        private async Task<BdObject> ProcessFileAsync(string filepath)
        {
            return await Task.Run(() =>
            {
                // read base64 text from file
                var base64Text = FileBrowserHelpers.ReadTextFromFile(filepath);

                // Base64 to gzipData
                var gzipData = Convert.FromBase64String(base64Text);

                // gzip to json
                var jsonData = DecompressGzip(gzipData);

#if UNITY_EDITOR
                Debug.Log(jsonData);
#endif

                // json to BdObject[]
                var bdObjects = JsonConvert.DeserializeObject<BdObject[]>(jsonData);
                BdObjectHelper.SetParent(null, bdObjects[0]);
                return bdObjects[0];
            });
        }

        // making display from file

        public async Task<AnimObject> MakeDisplay(string filepath, string fileName)
        {
            // get file
            var bdObject = await ProcessFileAsync(filepath);
            // add gameobject
            await bdObjManager.AddObject(bdObject);
            // add anim object
            return animObjList.AddAnimObject(fileName);
        }

        // gzipData to json string file
        private string DecompressGzip(byte[] gzipData)
        {
            using var compressedStream = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private List<string> SortFiles(IEnumerable<string> fileNames)
        {
            // if UseNameInfoExtract is true, sort files by f<number>
            var regex = new Regex(@"f(\d+)", RegexOptions.IgnoreCase);

            var matchedFiles = new List<(string fileName, int number)>();
            var unmatchedFiles = new List<string>();

            foreach (var fileName in fileNames)
            {
                var match = regex.Match(fileName);
                // find f<number> in the file name
                if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                {
                    matchedFiles.Add((fileName, number));
                }
                else
                {
                    unmatchedFiles.Add(fileName);
                }
            }

            // sort matched files by number
            matchedFiles.Sort((a, b) => a.number.CompareTo(b.number));

            // add unmatched files to the end
            var sortedFiles = matchedFiles.Select(x => x.fileName).ToList();
            sortedFiles.AddRange(unmatchedFiles);

            return sortedFiles;
        }
    }
}
