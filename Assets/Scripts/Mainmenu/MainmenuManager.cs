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

            StringBuilder sb = new StringBuilder();
            foreach (var version in MinecraftFileManager.SurportedVersions)
            {
                sb.Append(version + '\n');
            }
            supportVersionText.text = sb.ToString();

            string path = PlayerPrefs.GetString("MinecraftPath", string.Empty);
            if (string.IsNullOrEmpty(path))
            {
                string applicationPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var SurportedVersions = MinecraftFileManager.SurportedVersions;

                // Minecraft 폴더 찾기
                string minecraftFolder = Path.Combine(applicationPath, ".minecraft", "versions");

                if (Directory.Exists(minecraftFolder))
                {
                    // 가장 최신부터 찾기
                    foreach (var version in SurportedVersions)
                    {
                        string versionPath = Path.Combine(minecraftFolder, version, version + ".jar");
                        if (File.Exists(versionPath))
                        {
                            path = versionPath;
                            break;
                        }
                    }
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
                    foreach (var version in SurportedVersions)
                    {
                        string versionPath = Path.Combine(
                            prismLauncherPath,
                            "minecraft-" + version + "-client.jar"
                        );
                        if (File.Exists(versionPath))
                        {
                            path = versionPath;
                            break;
                        }
                    }

                }
            }
            // Debug.Log(path);
            bool isSuccess = await SetNewPath(path);

            if (!isSuccess)
            {
                // 파일을 못찾았을 경우 버전 패널을 띄움
                GetComponent<VersionLoadPanel>().OnPanelButton();
            }

            isFirstVisiting = true;
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