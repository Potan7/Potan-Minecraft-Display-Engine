using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TickLine : MonoBehaviour
{
    public TextMeshProUGUI text;
    [SerializeField]
    int _tick;
    public int Tick { get => _tick; }
    public Image image;
    public RectTransform rect;
    public int index;

    public void SetTick(int t, bool ForceLarge = false)
    {
        text.text = t.ToString();
        _tick = t;

        if (t % 10 == 0 || ForceLarge)
        {
            SetImageVertical(25);
            text.gameObject.SetActive(true);
        }
        else
        {
            SetImageVertical(10);
            text.gameObject.SetActive(false);
        }
    }

    public void SetImageVertical(float size)
    {
        image.rectTransform.sizeDelta = new Vector2(image.rectTransform.sizeDelta.x, size);
    }
}
