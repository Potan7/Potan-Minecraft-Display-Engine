using UnityEngine;

public class SystemManager : RootManager
{
    // Update is called once per frame
    void Update()
    {
        // paste from clipboard
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
        {
            try
            {
                string clipboard = GUIUtility.systemCopyBuffer;

                CustomLog.Log("Clipboard: " + clipboard);

                GameManager.GetManager<FileManager>().MakeDisplay(clipboard);
            }
            catch (System.Exception e)
            {
                CustomLog.Log("Clipboard is not BDEFile");
                CustomLog.LogError(e);
            }

        }
    }
}
