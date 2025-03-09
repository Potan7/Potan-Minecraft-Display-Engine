using UnityEngine;

public class CustomLog
{
    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#endif
        LogConsole.instance.Log(message);
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
        LogConsole.instance.Log(message, Color.red);
    }
}