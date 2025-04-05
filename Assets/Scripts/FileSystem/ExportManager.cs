using System.Collections;
using GameSystem;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;

namespace FileSystem
{
    public class ExportManager : MonoBehaviour
    {
        public GameObject exportPanel;

        public TextMeshProUGUI exportPathText;
        private string exportPath = "result";
        public string ExportPath
        {
            get => exportPath;
            set
            {
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
            currentPath = string.Concat(path, '/', exportPath);
            exportPathText.text = currentPath;
        }

        public void SetExportPanel(bool isShow)
        {
            exportPanel.SetActive(isShow);
            if (isShow)
            {
                SetPathText(currentPath);
                UIManager.CurrentUIStatus |= UIManager.UIStatus.OnPopupPanel;
            }
            else
            {
                UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnPopupPanel;
            }
        }

        public void ChangePath() => StartCoroutine(GetNewPathCoroutine());

        private IEnumerator GetNewPathCoroutine()
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, Application.dataPath);

            if (FileBrowser.Success)
            {
                //Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetPathText(FileBrowser.Result[0]);
            }
        }

        public void OnExportButton()
        {
            //Debug.Log("Exporting to: " + currentPath);
        }

        public void ExportAnimation()
        {
            
        }
    }
}