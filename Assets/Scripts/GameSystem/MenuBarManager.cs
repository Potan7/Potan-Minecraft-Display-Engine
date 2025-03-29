using System;
using UnityEngine;
using FileSystem;
using TMPro;

namespace GameSystem
{

    public class MenuBarManager : MonoBehaviour
    {
        private const string githubURL = "https://github.com/Potan7/Potan-Minecraft-Display-Engine";
        private ExportManager _exportManager;
        private SaveManager _saveManager;

        public TextMeshProUGUI currentFileText;

        public GameObject FilePanelButtons;
        public GameObject SavePanel;

        void Start()
        {
            _exportManager = GetComponent<ExportManager>();
            _saveManager = GameManager.GetManager<SaveManager>();

            GameManager.Setting.exportManager = _exportManager;
        }

        public void SetCurrentFileText(string fileName)
        {
            currentFileText.text = fileName;
        }
        
        #region  Button Events

        public void OnSaveButton()
        {
            if (_saveManager.IsNoneSaved)
            {
                // Save As 실행하기
                OnSaveAsButton();
            }
            else
            {
                // Save 하기
            }
        }

        public void OnSaveAsButton()
        {
            SavePanel.SetActive(true);
            UIManager.CurrentUIStatus |= UIManager.UIStatus.OnPopupPanel;
        }

        public void OnSavePanelCloseButton()
        {
            SavePanel.SetActive(false);
            UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnPopupPanel;
        }

        public void OnLoadButton()
        {
            CustomLog.Log("Load 기능 구현 안되었음");
        }

        public void OnExportButton()
        {
            _exportManager.SetExportPanel(true);
        }

        public void OnGitHubButton()
        {
            Application.OpenURL(githubURL);
        }
        #endregion

        #region Mouse Events

        public void OnMenuBarMouseEnter()
        {
            UIManager.CurrentUIStatus |= UIManager.UIStatus.OnMenuBarPanel;
        }

        public void OnMenuBarMouseExit()
        {
            if (FilePanelButtons.activeSelf) return;
            UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnMenuBarPanel;
        }

        public void OnFilesButtonMouseEnter()
        {
            FilePanelButtons.SetActive(true);
        }

        public void OnFilesButtonMouseExit()
        {
            FilePanelButtons.SetActive(false);
            UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnMenuBarPanel;
        }

        #endregion
    }
}
