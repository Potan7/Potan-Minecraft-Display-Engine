using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace GameSystem
{


    [System.Serializable]
    public class GitHubAsset
    {
        public string browser_download_url;
    }

    [System.Serializable]
    public class GitHubRelease
    {
        public string tag_name;
        public GitHubAsset[] assets;
    }

    public class VersionChecker : MonoBehaviour
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/Potan7/Potan-Minecraft-Display-Engine/releases/latest";

        public TextMeshProUGUI popupMenu;

        void Start()
        {
            StartCoroutine(CheckLatestVersion());
        }

        IEnumerator CheckLatestVersion()
        {
            UnityWebRequest request = UnityWebRequest.Get(GitHubApiUrl);
            request.SetRequestHeader("User-Agent", "UnityApp");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                GitHubRelease release = JsonUtility.FromJson<GitHubRelease>(json);

                string currentVersion = Application.version;
                string latestVersion = release.tag_name;

                if (latestVersion != currentVersion)
                {
                    CustomLog.Log($"새 버전이 있습니다! 현재: {currentVersion}, 최신: {latestVersion}");
                    ShowUpdatePopup(latestVersion);
                }
            }
            else
            {
                CustomLog.LogError("버전 확인 실패: " + request.error);
            }
        }

        void ShowUpdatePopup(string latestVersion)
        {
            popupMenu.gameObject.SetActive(true);
            popupMenu.text = $"새 버전이 있습니다! 최신 버전: {latestVersion}";
        }


    }
}