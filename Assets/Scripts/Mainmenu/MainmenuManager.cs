using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Minecraft;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LitMotion;
using LitMotion.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Mainmenu
{
    public class MainmenuManager : MonoBehaviour
    {
        MinecraftFileManager minecraftFileManager;
        public TextMeshProUGUI versionText;
        public TextMeshProUGUI versionErrorMsg;

        public bool isInstalled = false;

        public Button[] buttons;

        // public RawImage fadeImg;
        public CanvasGroup menu;

        public TextMeshProUGUI supportVersionText;

        const string backgroundColor = "#303030";
        // public RectTransform previewPanel;

        public static bool isFirstVisiting = false;

        // public static readonly Regex VersionRegex = new Regex(@"(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled);

        async void Start()
        {
            minecraftFileManager = MinecraftFileManager.Instance;

            string start = minecraftFileManager.surportedVersions.surportedVersionStart;
            string end = minecraftFileManager.surportedVersions.surportedVersionEnd;

            supportVersionText.SetText($"{start} ~ {end}");

            string path = PlayerPrefs.GetString("MinecraftPath", string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                string applicationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // Minecraft 폴더 찾기
                string minecraftFolder = Path.Combine(applicationPath, ".minecraft", "versions");

                if (Directory.Exists(minecraftFolder))
                {
                    // // 가장 최신부터 찾기
                    // foreach (var version in SurportedVersions)
                    // {
                    //     string versionPath = Path.Combine(minecraftFolder, version, version + ".jar");
                    //     if (File.Exists(versionPath))
                    //     {
                    //         path = versionPath;
                    //         break;
                    //     }
                    // }
                    path = GetLatestSupportedVersionPath(minecraftFolder);
                }
                else
                {
                    // Minecraft 폴더가 없으면 프리즘 런처를 수색함
                    //PrismLauncher/libraries\com\mojang\minecraft
                    string prismLauncherPath = Path.Combine(
                        applicationPath,
                        "PrismLauncher",
                        "libraries",
                        "com",
                        "mojang",
                        "minecraft"
                    );
                    // 프리즘의 jar 파일 이름은 minecraft-1.21.6-client.jar
                    // foreach (var version in SurportedVersions)
                    // {
                    //     string versionPath = Path.Combine(
                    //         prismLauncherPath,
                    //         "minecraft-" + version + "-client.jar"
                    //     );
                    //     if (File.Exists(versionPath))
                    //     {
                    //         path = versionPath;
                    //         break;
                    //     }
                    // }
                    if (Directory.Exists(prismLauncherPath))
                        path = GetLatestSupportedVersionPath(prismLauncherPath, true);

                }
            }
            // Debug.Log(path);
            bool isSuccess = await SetNewPath(path);

            if (!isSuccess)
            {
                // 파일을 못찾았을 경우 버전 패널을 띄움
                if (TryGetComponent<VersionLoadPanel>(out var versionLoadPanel))
                {
                    versionLoadPanel.OnPanelButton();
                }
            }

            isFirstVisiting = true;
        }

        // 해당 경로의 폴더 중 지원하는 버전이 있는지 확인
        string GetLatestSupportedVersionPath(string rootPath, bool isPrism = false)
        {
            if (!Directory.Exists(rootPath)) return null;

            // 해당 디렉토리의 모든 하위 디렉토리/파일 스캔
            string[] entries = isPrism ? Directory.GetFiles(rootPath) : Directory.GetDirectories(rootPath);

            List<(string path, Version version)> supportedVersions = new();

            // 버전 패턴과 일치하는 항목 필터링
            foreach (var entry in entries)
            {
                ReadOnlySpan<char> entrySpan = entry.AsSpan();
                // 일반: 버전 명이 폴더 이름, 프리즘: 파일이 minecraft-1.21.6-client.jar 형태
                var nameSpan = isPrism ? Path.GetFileNameWithoutExtension(entrySpan) : Path.GetFileName(entrySpan);

                if (isPrism)
                {
                    if (!nameSpan.StartsWith("minecraft-") || !nameSpan.EndsWith("-client"))
                        continue;

                    nameSpan = nameSpan[10..^7]; // "minecraft-" 제거 및 "-client" 제거
                }

                // 3. 버전이 지원되는지 확인
                if (MinecraftFileManager.Instance.IsVersionSupported(nameSpan))
                {
                    // 지원되는 버전이면 리스트에 추가
                    if (Version.TryParse(nameSpan, out Version parsedVersion))
                    {
                        supportedVersions.Add((entry, parsedVersion));
                    }
                }
            }

            var sortedList = supportedVersions.OrderByDescending(x => x.version);

            // 루프를 돌며 파일 확인
            foreach (var (path, version) in sortedList)
            {
                // 일반 런처: rootPath(versions) / 1.21.1(folder) / 1.21.1.jar
                // Prism: rootPath / minecraft-1.21.1-client.jar (이미 item.path에 들어있음)
                string jarPath = isPrism
                    ? path
                    : Path.Combine(path, version.ToString() + ".jar");

                if (File.Exists(jarPath))
                {
                    return jarPath; // 최신 버전을 찾아서 반환
                }
            }

            return null;
        }

        public async UniTask<bool> SetNewPath(string path)
        {
            const string versionPattern = @"(\d+)\.(\d+)\.(\d+)";

            string version = Regex.Match(path, versionPattern).Value;
            if (string.IsNullOrEmpty(version))
            {
                versionText.text = "File not found";
                versionErrorMsg.text = "File not found";
                buttons[0].interactable = false;
                buttons[1].interactable = false;
                return false;
            }

            string error;
            (isInstalled, error) = await minecraftFileManager.ReadMinecraftFile(path, version);

            buttons[0].interactable = isInstalled;
            buttons[1].interactable = isInstalled;

            if (!isInstalled)
            {
                versionText.text = error;
                versionErrorMsg.text = error;
                PlayerPrefs.SetString("MinecraftPath", string.Empty);

                return false;
            }

            versionText.text = "Version: " + version;
            PlayerPrefs.SetString("MinecraftPath", path);
            versionErrorMsg.text = string.Empty;
            PlayerPrefs.Save();
            return true;
        }

        public void OnAnimatorButton()
        {
            var loadScene = SceneManager.LoadSceneAsync("Animation");

            loadScene.allowSceneActivation = false;
            menu.interactable = false;

            LMotion.Create(1f, 0f, 0.5f)
                .WithEase(Ease.InOutBack)
                .WithOnComplete(() =>
                {
                    loadScene.allowSceneActivation = true;
                })
                .Bind(menu, (value, m) =>
                {
                    m.alpha = value;
                    m.transform.localScale = Vector3.one * value;
                });

            if (ColorUtility.TryParseHtmlString(backgroundColor, out Color color))
            {
                var cam = Camera.main;
                LMotion.Create(cam.backgroundColor, color, 0.5f)
                    .WithEase(Ease.InQuad)
                    .WithOnComplete(() =>
                    {
                        loadScene.allowSceneActivation = true;
                    })
                .Bind(cam, (value, c) => c.backgroundColor = value);
            }
        }

        public void OnDisplayMakerButton()
        {
            var loadScene = SceneManager.LoadSceneAsync("ModelCreator");

            loadScene.allowSceneActivation = false;
            menu.interactable = false;

            LMotion.Create(1f, 0f, 0.5f)
                .WithEase(Ease.InOutBack)
                .WithOnComplete(() =>
                {
                    loadScene.allowSceneActivation = true;
                })
                .Bind(menu, (value, m) =>
                {
                    m.alpha = value;
                    m.transform.localScale = Vector3.one * value;
                });

            if (ColorUtility.TryParseHtmlString(backgroundColor, out Color color))
            {
                var cam = Camera.main;
                LMotion.Create(cam.backgroundColor, color, 0.5f)
                    .WithEase(Ease.InQuad)
                    .WithOnComplete(() =>
                    {
                        loadScene.allowSceneActivation = true;
                    })
                .Bind(cam, (value, c) => c.backgroundColor = value);
            }
        }

        public void OnOpenGoogleFormButton()
        {
            Application.OpenURL("https://docs.google.com/forms/d/e/1FAIpQLSfZpYsPRXoxlwlYd3d9ZHFvd27EZ-ZFE9T20LnA31ig4AY6hA/viewform?usp=header");
        }
    }
}