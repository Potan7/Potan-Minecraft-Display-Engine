using JetBrains.Annotations;
using Riten.Native.Cursors;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using FileSystem;
using System;

namespace GameSystem
{
    public class UIManager : BaseManager
    {

        [Flags]
        public enum UIStatus {
            None = 0,
            OnAnimUIPanel = 1 << 0,
            OnSettingPanel = 1 << 1,
            OnPopupPanel = 1 << 2,
            OnDraggingPanel = 1 << 3,
            OnMenuBarPanel = 1 << 4,
        }
        
        private static UIStatus _currentUIStatus = UIStatus.None;
        public static UIStatus CurrentUIStatus 
        {
            get => _currentUIStatus; 
            set
            {
                _currentUIStatus = value;
                GameManager.SetPlayerInput((_currentUIStatus | UIStatus.OnMenuBarPanel | UIStatus.OnPopupPanel) != UIStatus.None);
                //Debug.Log($"CurrentUIStatus: {_currentUIStatus}");
            }
        }

        const string DefaultLoadingText = "Loading...";

        private FileLoadManager _fileManager;

        [FormerlySerializedAs("LoadingPanel")] public GameObject loadingPanel;
        public TextMeshProUGUI loadingText;

        private int _cursorID;

        private void Start()
        {
            _fileManager = GameManager.GetManager<FileLoadManager>();
            
            _currentUIStatus = UIStatus.None;
        }

        public void SetLoadingPanel(bool isOn)
        {
            loadingPanel.SetActive(isOn);
            if (isOn)
            {
                _cursorID = CursorStack.Push(NTCursors.Busy);
            }
            else
            {
                CursorStack.Pop(_cursorID);
                loadingText.text = DefaultLoadingText;
            }
        }

        public void SetLoadingText(string text)
        {
            loadingText.text = text;
        }

        [UsedImplicitly]
        public void OnPressImportButton()
        {
            _fileManager.ImportFile();
        }
    }
}
