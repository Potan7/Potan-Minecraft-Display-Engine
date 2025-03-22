using System.Collections;
using Manager;
using SimpleFileBrowser;
using UnityEngine;

namespace ToolSystem
{
    public class ExportManager : MonoBehaviour
    {
        public IEnumerator ExportCoroutine()
        {
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, FileLoadManager.DefaultPath);

            if (FileBrowser.Success)
            {
                Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
            }
        }
    }
}