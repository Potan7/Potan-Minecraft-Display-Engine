using JetBrains.Annotations;
using Riten.Native.Cursors;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using BDObjectSystem;

namespace Manager
{
    public class UIManger : BaseManager
    {
        private FileManager _fileManager;

        [FormerlySerializedAs("LoadingPanel")] public GameObject loadingPanel;
        private int _cursorID;

        private void Start()
        {
            _fileManager = GameManager.GetManager<FileManager>();
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
            }
        }

        [UsedImplicitly]
        public void OnPressImportButton()
        {
            _fileManager.ImportFile();
        }

        [UsedImplicitly]
        public void OnRealodButton()
        {
            DestroyImmediate(GameManager.Instance.gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
