//using B83.Win32;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SystemManager : RootManager
{
    public string[] filesDropped;

    protected override void Awake()
    {
        base.Awake();
        //UnityDragAndDropHook.InstallHook();
        //UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }

    //private void OnDestroy()
    //{
    //    UnityDragAndDropHook.UninstallHook();
    //}

    //public void OnFiles(List<string> aPathNames, POINT aDropPoint)
    //{
    //    GameManager.GetManager<FileManager>().AfterLoadFile(aPathNames.ToArray());
    //}

    // Update is called once per frame
    void Update()
    {
        // paste from clipboard
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
        {
            try
            {
                string clipboard = GUIUtility.systemCopyBuffer;

                //CustomLog.Log("Clipboard: " + clipboard);

                GameManager.GetManager<FileManager>().MakeDisplay(clipboard);
            }
            catch (System.Exception e)
            {
                CustomLog.Log("Clipboard is not BDEFile");
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
            }

        }
    }
}
