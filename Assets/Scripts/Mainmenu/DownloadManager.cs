using Cysharp.Threading.Tasks;
using Minecraft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Text.Json;
using System.Linq;
using System.IO;

namespace Mainmenu
{
    public class DownloadManager : MonoBehaviour
    {
        public TextMeshProUGUI versionText;
        public TextMeshProUGUI downloadButtonText;
        public Button downloadButton;

        void Awake()
        {
            string path = GetJarDirectory();
            if (Directory.Exists(path) && Directory.GetFiles(path).Length > 0)
            {
                string existingFile = Directory.GetFiles(path).FirstOrDefault();
                versionText.SetText(Path.GetFileNameWithoutExtension(existingFile));

                downloadButtonText.SetText("다시 다운로드");
            }
        }

        public async void OnClickDownloadButton()
        {
            string version = MinecraftFileManager.Instance.surportedVersions.surportedVersionEnd;

            string folderPath = GetJarDirectory();

            // 파일명 생성: "1.21.1.jar"
            string fileName = $"{version}.jar";
            string fullPath = Path.Combine(folderPath, fileName);

            CleanDirectory(folderPath);

            versionText.SetText(version);
            downloadButtonText.SetText("설치 중...");
            downloadButton.interactable = false;

            try
            {
                await DownloadMinecraftJar(version, fullPath);

                if (TryGetComponent<VersionLoadPanel>(out var versionLoadPanel))
                {
                    versionLoadPanel.SetPath(fullPath);
                    versionLoadPanel.OnChangePathButton();
                }
            }
            catch (System.Exception ex)
            {
                CustomLog.UnityLog($"Failed to download Minecraft {version} | {ex.Message}", isError: true);
                versionText.SetText($"{version} 다운로드 실패");
            }
            finally
            {
                downloadButtonText.SetText("다시 다운로드");
                downloadButton.interactable = true;
            }
        }

        async UniTask DownloadMinecraftJar(string targetVersion, string savePath)
        {
            using var client = new HttpClient();

            // 1. 전체 버전 목록 가져오기
            var manifestJson = await client.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            using var manifest = JsonDocument.Parse(manifestJson);

            // 2. targetVersion에 맞는 정보 URL 찾기
            var versionEntry = manifest.RootElement.GetProperty("versions")
                .EnumerateArray()
                .FirstOrDefault(v => v.GetProperty("id").GetString() == targetVersion);

            string infoUrl = versionEntry.GetProperty("url").GetString();

            // 3. 해당 버전의 상세 정보(jar 링크 등) 가져오기
            var infoJson = await client.GetStringAsync(infoUrl);
            using var info = JsonDocument.Parse(infoJson);

            string jarUrl = info.RootElement
                .GetProperty("downloads")
                .GetProperty("client")
                .GetProperty("url").GetString();

            // 4. .jar 파일 다운로드 및 저장
            var jarBytes = await client.GetByteArrayAsync(jarUrl);
            await File.WriteAllBytesAsync(savePath, jarBytes);
        }

        private string GetJarDirectory()
        {
            // 1. 실행 파일 위치 근처에 'GameData/Jars' 폴더 경로 생성
            string root = Directory.GetParent(Application.dataPath).FullName;
            string folderPath = Path.Combine(root, "DownloadedJars");

            // 폴더가 없으면 생성
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            return folderPath;
        }


        private void CleanDirectory(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }
    }
}