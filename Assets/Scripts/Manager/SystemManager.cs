//using B83.Win32;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SystemManager : BaseManager
{
    public string[] filesDropped;


    private float deltaTime = 0f;

    [SerializeField] private int size = 15;
    [SerializeField] private Color color = Color.white;

    GUIStyle style;

    protected override void Awake()
    {
        base.Awake();

        Application.targetFrameRate = 165;

        //UnityDragAndDropHook.InstallHook();
        //UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }

    private void Start()
    {
        style = new GUIStyle();

        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = size;
        style.normal.textColor = color;
    }

    private void OnGUI()
    {


        Rect rect = new Rect(10, 30, Screen.width, Screen.height);

        float ms = deltaTime * 1000f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.} FPS ({1:0.0} ms)", fps, ms);
        
        Rect versionRect = new Rect(10, 10, Screen.width, Screen.height);
        string version = string.Format("Version: {0}", Application.version);

        GUI.Label(rect, text, style);
        GUI.Label(versionRect, version, style);
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
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // paste from clipboard
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.V))
        {
            try
            {
                string clipboard = GUIUtility.systemCopyBuffer;

                CustomLog.Log("Clipboard: " + clipboard);

                _ = GameManager.GetManager<FileManager>().MakeDisplay(clipboard);
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
