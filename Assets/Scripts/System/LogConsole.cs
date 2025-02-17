using System.Collections;
using TMPro;
using UnityEngine;

public class LogConsole : MonoBehaviour
{
    public static LogConsole instance;
    public TextMeshProUGUI[] text;

    int index = 0;

    WaitForSeconds wait;

    private void Awake()
    {
        instance = this;
        wait = new WaitForSeconds(5f);
    }

    public void Log(object message, Color co)
    {
        text[index].text = message.ToString();
        text[index].color = co;
        StartCoroutine(TextCoroutine(text[index]));

        index = (index + 1) % text.Length;
    }

    public void Log(object message)
    {
        Log(message, Color.white);
    }

    IEnumerator TextCoroutine(TextMeshProUGUI txt)
    {
        txt.gameObject.SetActive(true);
        yield return wait;
        txt.color = Color.clear;
        txt.gameObject.SetActive(false);
    }
}
