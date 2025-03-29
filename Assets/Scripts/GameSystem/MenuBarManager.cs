using System;
using UnityEngine;
using FileSystem;
using TMPro;
using System.Collections;
using SimpleFileBrowser;
using UnityEngine.UI;

namespace GameSystem
{

    public class MenuBarManager : BaseManager
    {
        private const string githubURL = "https://github.com/Potan7/Potan-Minecraft-Display-Engine";
        private ExportManager _exportManager;
        private SaveManager _saveManager;

        public TextMeshProUGUI currentFileText;

        public GameObject FilePanelButtons;

        public Button[] saveButtons;

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

        private void UpdateSaveButtonInteraction()
        {
            foreach (var button in saveButtons)
            {
                button.interactable = _saveManager.IsNoneSaved;
            }
        }
        
        #region  Button Events

        public void OnSaveButton()
        {
            if (string.IsNullOrEmpty(_saveManager.MDEFilePath))
            {
                // Save As 실행하기
                OnSaveAsButton();
            }
            else
            {
                _saveManager.SaveMDEFile();
            }
        }

        public void OnSaveAsButton()
        {
            _saveManager.SaveAsNewFile();
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
            UpdateSaveButtonInteraction();
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
