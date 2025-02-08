using UnityEngine;
using System.Collections;
using SimpleFileBrowser;
using System.IO;
using System.Text;
using System;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Collections.Generic;

public class FileManager : RootManager
{
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

    IEnumerator ShowLoadDialogCoroutine(Action<string[]> callback)
    {
        // 파일 브라우저를 열고 사용자가 파일을 선택하거나 취소할 때까지 대기
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, true, null, null, "Select Files", "Load");

        // 파일 브라우저가 파일을 불러오면 콜백 함수 호출
        if (FileBrowser.Success)
            // 만약 Success가 false라면 Result는 null이 된다.
            callback?.Invoke(FileBrowser.Result); 
        else
        {
            Debug.Log("Failed to load file");
        }
    }

    public void ImportFile()
    {
        StartCoroutine(ShowLoadDialogCoroutine(AfterLoadFile));
    }

    void AfterLoadFile(string[] filepaths)
    {
        foreach (var path in filepaths)
        {
            // 1. 파일 읽어서 string 변환
            byte[] file = FileBrowserHelpers.ReadBytesFromFile(path);
            string base64Data = Encoding.UTF8.GetString(file);

            // 2. Base64 디코딩
            byte[] gzipData = Convert.FromBase64String(base64Data);
            // 3. GZip 압축 해제
            string jsonData = DecompressGzip(gzipData);

            // 4. JSON 데이터 출력
            Debug.Log(jsonData);

            MakeDisplay(jsonData);
        }
    }

    // JSON 데이터를 BDObject로 변환해서 오브젝트 생성
    public void MakeDisplay(string jsonData) => 
        GameManager.GetManager<BDObjectManager>()
        .AddObjects(
            JsonConvert.DeserializeObject<BDObject[]>(jsonData)
            );

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
