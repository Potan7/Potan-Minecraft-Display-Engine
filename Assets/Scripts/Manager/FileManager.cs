using UnityEngine;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FileManager : BaseManager
{
    public BDObjectManager bdObjManager;
    public AnimObjList animObjList;
    public HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

    private void Start()
    {
        bdObjManager = GameManager.GetManager<BDObjectManager>();

        // .bdengine, .bdstudio 확장자만 필터링
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

        // 런처 경로 추가
#if UNITY_EDITOR
        FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath);
#else
        FileBrowser.AddQuickLink("Launcher Folder", Application.dataPath + "/../");
#endif

        // 다운로드 폴더 추가
        string download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        download = Path.Combine(download, "Downloads");

        FileBrowser.AddQuickLink("Downloads", download);
    }

    IEnumerator ShowLoadDialogCoroutine(Action<List<string>> callback)
    {
        // 파일 브라우저를 열고 사용자가 파일을 선택하거나 취소할 때까지 대기
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Select Files", "Load");

        // 파일 브라우저가 파일을 불러오면 콜백 함수 호출
        if (FileBrowser.Success)
            // 만약 Success가 false라면 Result는 null이 된다.
        { 
            // 폴더 분리하기
            List<string> files = new List<string>();
            string[] result = FileBrowser.Result;
            for (int i = 0; i < result.Length; i++)
            {
                // 폴더 내 모든 파일들 리스트에 추가
                if (Directory.Exists(result[i]))
                {
                    string[] folderFiles = Directory.GetFiles(result[i], "*.bdengine", SearchOption.AllDirectories);

                    files.AddRange(folderFiles);
                }
                else
                {
                    // 아니라면 그냥 추가
                    files.Add(result[i]);
                }
            }

            callback?.Invoke(files); 
        }
        else
        {
            CustomLog.Log("Failed to load file");
        }
        
    }

    // 파일 임포트
    public void ImportFile()
    {
        StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));
    }

    // 프레임 임포트
    public void ImportFrame(AnimObject target, int tick)
    {
        StartCoroutine(ShowLoadDialogCoroutine((filepaths) => AfterLoadFrame(filepaths[0], target, tick)));
    }

    // 파일 불러와서 디스플레이 생성하기
    public async void AfterLoadFile(List<string> filepaths)
    {
        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        //List<Task> runningTasks = new List<Task>();

        // 첫번째 파일로 디스플레이 생성
        AnimObject animObject = await MakeDisplay(filepaths[0]);

        // 이후 파일부터는 프레임 추가하기
        for (int i = 1; i < filepaths.Count; i++)
        {
            BDObject[] bdObjects = await ProcessFileAsync(filepaths[i]);
            animObject.AddFrame(bdObjects[0], Path.GetFileNameWithoutExtension(filepaths[i]));
        }
        while (WorkingGenerators.Count > 0) await Task.Delay(500);


        //for (int i = 0; i < filepaths.Count; i++)
        //{
        //    runningTasks.Add(MakeDisplay(filepaths[i]));
        //    // 만약 파일이 너무 많으면 나눠서 작업
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

        CustomLog.Log($"Import is Done! BDObject Count: {GameManager.GetManager<BDObjectManager>().BDObjectCount}");
    }

    // 파일 불러와서 프레임 생성하기
    public async void AfterLoadFrame(string filepath, AnimObject target, int tick)
    {
        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        BDObject[] bdObjects = await ProcessFileAsync(filepath);
        target.AddFrame(Path.GetFileNameWithoutExtension(filepath), bdObjects[0], tick);

        GameManager.GetManager<UIManger>().SetLoadingPanel(false);
    }

    // 개별 파일 처리 비동기 함수
    private async Task<BDObject[]> ProcessFileAsync(string filepath)
    {
        return await Task.Run(() =>
        {
            // 파일 읽기(텍스트)
            string base64Text = FileBrowserHelpers.ReadTextFromFile(filepath);

            // Base64 → gzipData
            byte[] gzipData = Convert.FromBase64String(base64Text);

            // gzip 해제 → json
            string jsonData = DecompressGzip(gzipData);

#if UNITY_EDITOR
            Debug.Log(jsonData);
#endif

            // BDObject[] 역직렬화
            return JsonConvert.DeserializeObject<BDObject[]>(jsonData);
        });
    }

    // 디스플레이 만들기
    public async Task<AnimObject> MakeDisplay(string filepath)
    {
        BDObject[] bdObjects = await ProcessFileAsync(filepath);

        string fileName = Path.GetFileNameWithoutExtension(filepath);
        var bdobjCon = await bdObjManager.AddObject(bdObjects[0], fileName);
        return animObjList.AddAnimObject(bdobjCon, fileName);
    }

    // gzipData를 string으로 변환하기
    string DecompressGzip(byte[] gzipData)
    {
        using (var compressedStream = new MemoryStream(gzipData))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        using (var reader = new StreamReader(gzipStream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}
