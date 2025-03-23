using System.Collections;
using Manager;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace ToolSystem
{
    public class ExportManager : MonoBehaviour
    {
        public GameObject exportPanel;

        public TextMeshProUGUI exportPathText;
        private string exportPath = "result";
        public string ExportPath {
            get => exportPath;
            set {
                exportPath = value;
                SetPathText(currentPath);
            }
        }
        private string currentPath;

        private void Start()
        {
            SetPathText(Application.dataPath);
        }

        private void SetPathText(string path)
        {
            currentPath = path;
            exportPathText.text = (path + '/' + exportPath).Replace('\\', '/');
        }

        public void SetExportPanel(bool isShow)
        {
            exportPanel.SetActive(isShow);
            if (isShow)
                SetPathText(currentPath);
        }

        public void ChangePath() => StartCoroutine(GetNewPathCoroutine());

        private IEnumerator GetNewPathCoroutine()
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, Application.dataPath);

            if (FileBrowser.Success)
            {
                Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetPathText(FileBrowser.Result[0]);
            }
        }

        public void OnExportButton()
        {
            
        }
    }
}