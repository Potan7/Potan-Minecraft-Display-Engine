using UnityEngine;

public class CustomLog
{
    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        Debug.Log(message);
        LogConsole.instance.Log(message);
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
        Debug.LogError(message);
        LogConsole.instance.Log(message, Color.red);
    }
}