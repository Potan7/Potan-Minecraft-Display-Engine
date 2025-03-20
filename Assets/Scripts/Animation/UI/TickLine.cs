using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Animation.UI
{
    public class TickLine : MonoBehaviour
    {
        public TextMeshProUGUI text;
        [FormerlySerializedAs("_tick")] [SerializeField] private int tick;
        public int Tick => tick;
        public Image image;
        public RectTransform rect;
        public int index;

        public void SetTick(int t, bool forceLarge = false)
        {
            text.text = t.ToString();
            tick = t;

            if (t % 10 == 0 || forceLarge)
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
}
