using System;
using UnityEngine;

namespace ToolSystem
{

    public class MenuBarManager : MonoBehaviour
    {
        private const string githubURL = "https://github.com/Potan7/Potan-Minecraft-Display-Engine";
        ExportManager _exportManager;

        void Start()
        {
            _exportManager = GetComponent<ExportManager>();
        }

        public void OnSaveButton()
        {
            CustomLog.Log("Save 기능 구현 안되었음");
        }

        public void OnLoadButton()
        {
            CustomLog.Log("Load 기능 구현 안되었음");
        }

        public void OnExportButton()
        {
            _exportManager.SetExportPanel(true);
        }

        public void OnExitButton()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

        public void OnGitHubButton()
        {
            Application.OpenURL(githubURL);
        }
    }
}
