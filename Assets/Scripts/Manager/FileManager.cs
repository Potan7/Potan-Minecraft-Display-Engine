using UnityEngine;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

public class FileManager : RootManager
{
    public HashSet<HeadGenerator> WorkingGenerators = new HashSet<HeadGenerator>();

    private void Start()
    {
        // .bdengine, .bdstudio 확장자만 필터링
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Files", ".bdengine", ".bdstudio"));

        // 런처 경로 추가
        FileBrowser.AddQuickLink("Launcher File", Application.dataPath);

        // 다운로드 폴더 추가
        string download = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        download = Path.Combine(download, "Downloads");

        FileBrowser.AddQuickLink("Downloads", download);
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        BDEngineStyleCameraMovement.CanMoveCamera = false;
        // 파일 브라우저를 열고 사용자가 파일을 선택하거나 취소할 때까지 대기
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load");

        // 파일 브라우저가 파일을 불러오면 콜백 함수 호출
        if (FileBrowser.Success)
            // 만약 Success가 false라면 Result는 null이 된다.
            AfterLoadFile(FileBrowser.Result); 
        else
        {
            CustomLog.Log("Failed to load file");
            BDEngineStyleCameraMovement.CanMoveCamera = true;
        }
        
    }

    public void ImportFile()
    {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    public async void AfterLoadFile(string[] filepaths)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        GameManager.GetManager<UIManger>().SetLoadingPanel(true);

        Task[] tasks = new Task[filepaths.Length];

        for (int i = 0; i < filepaths.Length; i++)
        {
            tasks[i] = ProcessFileAsync(filepaths[i]);
        }
        await Task.WhenAll(tasks);
        await WaitWhileAsync(() => WorkingGenerators.Count > 0);
        stopwatch.Stop();

        GameManager.GetManager<UIManger>().SetLoadingPanel(false);
        BDEngineStyleCameraMovement.CanMoveCamera = true;

        CustomLog.Log($"BDObject Count: {GameManager.GetManager<BDObjectManager>().BDObjectCount}, Import Time: {stopwatch.ElapsedMilliseconds}ms");
    }

    public async Task WaitWhileAsync(Func<bool> conditionFunc, int checkIntervalMs = 500)
    {
        while (conditionFunc())
        {
            await Task.Delay(checkIntervalMs); // 지정한 시간(ms)만큼 대기 후 다시 체크
        }
    }

    // 개별 파일 처리 비동기 함수
    private async Task ProcessFileAsync(string filepath)
    {
        // 1. 파일 읽기 (비동기)
        byte[] file = await Task.Run(() => FileBrowserHelpers.ReadBytesFromFile(filepath));

        // 2. Base64 디코딩
        byte[] gzipData = Convert.FromBase64String(Encoding.UTF8.GetString(file));

        // 3. GZip 압축 해제 (비동기)
        string jsonData = await Task.Run(() => DecompressGzip(gzipData));

        // 4. JSON 데이터를 BDObject로 변환 및 오브젝트 생성
        await MakeDisplay(jsonData);
    }

    // JSON 데이터를 BDObject로 변환해서 오브젝트 생성
    public async Task MakeDisplay(string jsonData)
    {
        BDObject[] bDObjects = await Task.Run(() => JsonConvert.DeserializeObject<BDObject[]>(jsonData));

        await GameManager.GetManager<BDObjectManager>().AddObjects(bDObjects);
    }


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
