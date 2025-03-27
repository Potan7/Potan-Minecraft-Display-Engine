using System.Collections;
using TMPro;
using UnityEngine;

namespace ToolSystem
{
    public class LogConsole : MonoBehaviour
    {
        public static LogConsole instance;
        public TextMeshProUGUI[] text;

        private int _index;

        private WaitForSeconds _wait;

        private void Awake()
        {
            instance = this;
            _wait = new WaitForSeconds(5f);
        }

        public void Log(object message, Color co)
        {
            text[_index].text = message.ToString();
            text[_index].color = co;
            StartCoroutine(TextCoroutine(text[_index]));

            _index = (_index + 1) % text.Length;
        }

        public void Log(object message)
        {
            Log(message, Color.white);
        }

        private IEnumerator TextCoroutine(TextMeshProUGUI txt)
        {
            txt.gameObject.SetActive(true);
            yield return _wait;
            txt.color = Color.clear;
            txt.gameObject.SetActive(false);
        }
    }
}
