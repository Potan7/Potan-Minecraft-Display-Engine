using System.Collections;
using CameraMovement;
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

        private AnimExporter exporter;

        private void Start()
        {
            SetPathText(Application.dataPath);
            exporter = new AnimExporter();
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
                BdEngineStyleCameraMovement.CurrentCameraStatus |= BdEngineStyleCameraMovement.CameraStatus.OnExportPanel;
            }
            else
            {
                BdEngineStyleCameraMovement.CurrentCameraStatus &= ~BdEngineStyleCameraMovement.CameraStatus.OnExportPanel;
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
            exporter.ExportAnimation(currentPath);
        }
    }
}