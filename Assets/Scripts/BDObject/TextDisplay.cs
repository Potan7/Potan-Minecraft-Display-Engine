using Newtonsoft.Json.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class TextDisplay : DisplayObject
{
    [System.Serializable]
    public struct Option
    {
        public Color color;
        public float alpha;
        public Color backgroundColor;
        public float backgroundAlpha;
        public bool bold;
        public bool italic;
        public bool underlined;
        public bool strikethrough;
        public bool obfuscated;
        public float lineLength;
        public string align;

        public Option(JObject opt)
        {
            color = HexToColor(opt.TryGetValue("color", out JToken value) ? value.ToString() : "#FFFFFF");
            alpha = opt.TryGetValue("alpha", out value) ? float.Parse(value.ToString()) : 1;
            backgroundColor = HexToColor(opt.TryGetValue("backgroundColor", out value) ? value.ToString() : "#000000");
            backgroundAlpha = opt.TryGetValue("backgroundAlpha", out value) ? float.Parse(value.ToString()) : 1;
            bold = opt.TryGetValue("bold", out value) ? bool.Parse(value.ToString()) : false;
            italic = opt.TryGetValue("italic", out value) ? bool.Parse(value.ToString()) : false;
            underlined = opt.TryGetValue("underline", out value) ? bool.Parse(value.ToString()) : false;
            strikethrough = opt.TryGetValue("strikeThrough", out value) ? bool.Parse(value.ToString()) : false;
            obfuscated = opt.TryGetValue("obfuscated", out value) ? bool.Parse(value.ToString()) : false;
            lineLength = opt.TryGetValue("lineLength", out value) ? float.Parse(value.ToString()) : 50;
            align = opt.TryGetValue("align", out value) ? value.ToString() : "center";
        }
    }

    public TextMeshPro textMesh;
    public MeshRenderer background;

    public BDObject bdObject;
    public Option option;

    const float margin = 0.05f;

    public void Init(BDObject obj)
    {
        bdObject = obj;

        // 옵션 설정
        option = new Option(obj.options);

        SetText();

        RectTransform rt = textMesh.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(textMesh.preferredWidth * 0.9f, textMesh.preferredHeight * 0.9f);

        // 배경 설정
        background.transform.localScale = new Vector3(textMesh.preferredWidth + margin, textMesh.preferredHeight + margin, background.transform.localScale.z);
        background.transform.localPosition = new Vector3(0, textMesh.preferredHeight, background.transform.localPosition.z);
        background.material.color = new Color(
            option.backgroundColor.r,
            option.backgroundColor.g,
            option.backgroundColor.b,
            option.backgroundAlpha
            );


    }

    private void SetText()
    {
        // 텍스트 설정
        textMesh.text = InsertCharacterBasedLineBreaks(bdObject.name, option.lineLength);
        textMesh.color = new Color(
            option.color.r,
            option.color.g,
            option.color.b,
            option.alpha
            );
        textMesh.fontStyle = (option.bold ? FontStyles.Bold : FontStyles.Normal) |
                             (option.italic ? FontStyles.Italic : FontStyles.Normal) |
                             (option.underlined ? FontStyles.Underline : FontStyles.Normal) |
                             (option.strikethrough ? FontStyles.Strikethrough : FontStyles.Normal);

        //Debug.Log("fontStyle: " + textMesh.fontStyle);

        textMesh.alignment =
            option.align == "center" ? TextAlignmentOptions.Bottom :
            option.align == "right" ? TextAlignmentOptions.BottomRight :
            TextAlignmentOptions.BottomLeft;

        textMesh.transform.localPosition = new Vector3(0, textMesh.preferredHeight / 2.0f, 0);
    }

    string InsertCharacterBasedLineBreaks(string text, float maxWidth)
    {
        StringBuilder sb = new StringBuilder();
        float add = 1f;
        if (maxWidth >= 7)
        {
            add = 0.5f;
        }
        float count = 0f;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                sb.Append(c);
                count = 0f;
                continue;
            }

            sb.Append(c);

            if (c == ' ') count += add;
            else count++;

            if (count >= maxWidth)
            {
                sb.Append("\n"); // 줄바꿈 삽입
                count = 0f;
            }
        }

        return sb.ToString();
    }

    public static Color HexToColor(string hex)
    {
        // Unity의 ColorUtility.TryParseHtmlString()을 사용하여 변환
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        else
        {
            Debug.LogError("Invalid HEX Color: " + hex);
            return Color.white; // 기본값으로 White 반환
        }
    }
}
