using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Animation;
using BDObject;
using Newtonsoft.Json;
using SimpleFileBrowser;
using UnityEngine;

namespace Manager
{
    public class FileManager : BaseManager
    {
        public BdObjectManager bdObjManager;
        public AnimObjList animObjList;
        public HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

        public Dictionary<string, (int, int)> FrameInfo = new Dictionary<string, (int, int)>();

        private void Start()
        {
            bdObjManager = GameManager.GetManager<BdObjectManager>();

            // .bdengine, .bdstudio Ȯ���ڸ� ���͸�
            FileBrowser.SetFilters(false,
                new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

            // ��ó ��� �߰�
#if UNITY_EDITOR
            FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath);
#else
        FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");
#endif

            // �ٿ�ε� ���� �߰�
            var download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            download = Path.Combine(download, "Downloads");

            FileBrowser.AddQuickLink("Downloads", download);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator ShowLoadDialogCoroutine(Action<List<string>> callback)
        {
            // ���� �������� ���� ����ڰ� ������ �����ϰų� ����� ������ ���
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Select Files", "Load");

            // ���� �������� ������ �ҷ����� �ݹ� �Լ� ȣ��
            if (FileBrowser.Success)
                // ���� Success�� false��� Result�� null�� �ȴ�.
            { 
                // ���� �и��ϱ�
                var files = new List<string>();
                var result = FileBrowser.Result;

                foreach (var t in result)
                {
                    // ���� �� ��� ���ϵ� ����Ʈ�� �߰�
                    if (Directory.Exists(t))
                    {
                        var folderFiles = Directory.GetFiles(t, "*.bdengine", SearchOption.TopDirectoryOnly);

                        var settingManager = GameManager.GetManager<SettingManager>();

                        // frame.txt ������ �ִ��� Ȯ��
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
                        // �ƴ϶�� �׳� �߰�
                        files.Add(t);
                    }
                }

                callback?.Invoke(files); 
            }
            else
            {
                CustomLog.Log("Failed to load file");
            }
        
        }

        private void SetDictByFrameTxt(SettingManager settingManager, string frameFile)
        {
            CustomLog.Log("Frame.txt ���� ã�� : " + frameFile);
            FrameInfo.Clear();

            // ���� �б�
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

        // ���� ����Ʈ
        public void ImportFile() => StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));

        // ������ ����Ʈ
        public void ImportFrame(AnimObject target, int tick) => StartCoroutine(FrameImportCoroutine(target, tick));

        private IEnumerator FrameImportCoroutine(AnimObject target, int tick)
        {
            // ���� �������� ���� ����ڰ� ������ �����ϰų� ����� ������ ���
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

        // ���� �ҷ��ͼ� ���÷��� �����ϱ�
        public async void AfterLoadFile(List<string> filepaths)
        {
            GameManager.GetManager<UIManger>().SetLoadingPanel(true);

            var settingManager = GameManager.GetManager<SettingManager>();
            if (settingManager.UseFrameTxtFile || settingManager.UseNameInfoExtract)
            {
                // ���ϸ� ������ ����
                filepaths = SortFiles(filepaths);
                //Debug.Log("Sorted Files : ");
                //for (int i = 0; i < filepaths.Count; i++)
                //{
                //    Debug.Log(filepaths[i]);
                //}
            }

            // ù��° ���Ϸ� ���÷��� ����
            var animObject = await MakeDisplay(filepaths[0]);

            // ���� ���Ϻ��ʹ� ������ �߰��ϱ�
            for (var i = 1; i < filepaths.Count; i++)
            {
                var bdObjects = await ProcessFileAsync(filepaths[i]);
                animObject.AddFrame(bdObjects[0], Path.GetFileNameWithoutExtension(filepaths[i]));
            }
            while (WorkingGenerators.Count > 0) await Task.Delay(500);


            //for (int i = 0; i < filepaths.Count; i++)
            //{
            //    runningTasks.Add(MakeDisplay(filepaths[i]));
            //    // ���� ������ �ʹ� ������ ������ �۾�
            //    if (runningTasks.Count >= batch)
            //    {
            //        await Task.WhenAll(runningTasks);
            //        runningTasks.Clear();
            //    }
            //}
            //if (runningTasks.Count > 0)
            //{
            //    await Task.WhenAll(runningTasks);
            //}

            GameManager.GetManager<UIManger>().SetLoadingPanel(false);

            CustomLog.Log($"Import is Done! BDObject Count: {GameManager.GetManager<BdObjectManager>().bdObjectCount}");
        }

        // ���� �ҷ��ͼ� ������ �����ϱ�
        public async void AfterLoadFrame(string filepath, AnimObject target, int tick)
        {
            GameManager.GetManager<UIManger>().SetLoadingPanel(true);

            var bdObjects = await ProcessFileAsync(filepath);
            target.AddFrame(Path.GetFileNameWithoutExtension(filepath), bdObjects[0], tick, GameManager.Setting.defaultInterpolation);

            GameManager.GetManager<UIManger>().SetLoadingPanel(false);
        }

        // ���� ���� ó�� �񵿱� �Լ�
        private static async Task<BdObject[]> ProcessFileAsync(string filepath)
        {
            return await Task.Run(() =>
            {
                // ���� �б�(�ؽ�Ʈ)
                var base64Text = FileBrowserHelpers.ReadTextFromFile(filepath);

                // Base64 �� gzipData
                var gzipData = Convert.FromBase64String(base64Text);

                // gzip ���� �� json
                var jsonData = DecompressGzip(gzipData);

#if UNITY_EDITOR
                Debug.Log(jsonData);
#endif

                // BDObject[] ������ȭ
                return JsonConvert.DeserializeObject<BdObject[]>(jsonData);
            });
        }

        // ���÷��� �����
        public async Task<AnimObject> MakeDisplay(string filepath)
        {
            var bdObjects = await ProcessFileAsync(filepath);

            var fileName = Path.GetFileNameWithoutExtension(filepath);
            await bdObjManager.AddObject(bdObjects[0], fileName);
            return animObjList.AddAnimObject(fileName);
        }

        // gzipData�� string���� ��ȯ�ϱ�
        private static string DecompressGzip(byte[] gzipData)
        {
            using var compressedStream = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        public List<string> SortFiles(IEnumerable<string> fileNames)
        {
            // "f" �ڿ� ���� ���ڸ� �����ϴ� ���Խ� (��γ� �����ڿ� �����ϰ�)
            var regex = new Regex(@"f(\d+)", RegexOptions.IgnoreCase);

            var matchedFiles = new List<(string fileName, int number)>();
            var unmatchedFiles = new List<string>();

            foreach (var fileName in fileNames)
            {
                var match = regex.Match(fileName);
                // f ���� ���ڸ� ����
                if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                {
                    matchedFiles.Add((fileName, number));
                }
                else
                {
                    unmatchedFiles.Add(fileName);
                }
            }

            // "f����"�� �ִ� ������ ���� �������� ����
            matchedFiles.Sort((a, b) => a.number.CompareTo(b.number));

            // ���ĵ� ����Ʈ + ������ ���� ������ �ڿ� �߰�
            var sortedFiles = matchedFiles.Select(x => x.fileName).ToList();
            sortedFiles.AddRange(unmatchedFiles);

            return sortedFiles;
        }
    }
}
