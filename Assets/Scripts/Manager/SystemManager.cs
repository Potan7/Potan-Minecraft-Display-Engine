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

                GameManager.GetManager<FileManager>().MakeDisplay(clipboard);
            }
            catch (System.Exception e)
            {
                Debug.Log("Clipboard is not BDEFile");
                Debug.LogError(e);
            }

        }
    }
}
