using System;
using UnityEngine;

public class MenuBarManager : MonoBehaviour
{
    public void OnSaveButton()
    {
        CustomLog.Log("Save 기능 구현 안되었음");
    }

    public void OnLoadButton()
    {
        CustomLog.Log("Load 기능 구현 안되었음");
    }

    public void OnExportButton()
    {
        CustomLog.Log("Export 기능 구현 안되었음");
    }

    public void OnExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnGitHubButton()
    {
        Application.OpenURL("https://github.com/Potan7/Potan-Minecraft-Animation-Viewer");
    }
}
