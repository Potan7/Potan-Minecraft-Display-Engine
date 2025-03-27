using JetBrains.Annotations;
using Riten.Native.Cursors;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using BDObjectSystem;
using TMPro;

namespace Manager
{
    public class UIManger : BaseManager
    {
        const string DefaultLoadingText = "Loading...";

        private FileLoadManager _fileManager;

        [FormerlySerializedAs("LoadingPanel")] public GameObject loadingPanel;
        public TextMeshProUGUI loadingText;

        private int _cursorID;

        private void Start()
        {
            _fileManager = GameManager.GetManager<FileLoadManager>();
            GameManager.GetManager<BdObjectManager>();
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
