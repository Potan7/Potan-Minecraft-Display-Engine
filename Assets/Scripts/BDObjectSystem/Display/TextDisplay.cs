using System;
using System.Text;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace BDObjectSystem.Display
{
    public class TextDisplay : DisplayObject
    {
        [Serializable]
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
                color = HexToColor(opt.TryGetValue("color", out var value) ? value.ToString() : "#FFFFFF");
                alpha = opt.TryGetValue("alpha", out value) ? float.Parse(value.ToString()) : 1;
                backgroundColor = HexToColor(opt.TryGetValue("backgroundColor", out value) ? value.ToString() : "#000000");
                backgroundAlpha = opt.TryGetValue("backgroundAlpha", out value) ? float.Parse(value.ToString()) : 1;
                bold = opt.TryGetValue("bold", out value) && bool.Parse(value.ToString());
                italic = opt.TryGetValue("italic", out value) && bool.Parse(value.ToString());
                underlined = opt.TryGetValue("underline", out value) && bool.Parse(value.ToString());
                strikethrough = opt.TryGetValue("strikeThrough", out value) && bool.Parse(value.ToString());
                obfuscated = opt.TryGetValue("obfuscated", out value) && bool.Parse(value.ToString());
                lineLength = opt.TryGetValue("lineLength", out value) ? float.Parse(value.ToString()) : 50;
                align = opt.TryGetValue("align", out value) ? value.ToString() : "center";
            }
        }

        public TextMeshPro textMesh;
        public MeshRenderer background;

        public BdObject BdObject;
        public Option option;

        private const float Margin = 0.05f;

        public void Init(BdObject obj)
        {
            BdObject = obj;

            // �ɼ� ����
            option = new Option(obj.Options);

            SetText();

            var rt = textMesh.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(textMesh.preferredWidth * 0.9f, textMesh.preferredHeight * 0.9f);

            // ��� ����
            background.transform.localScale = new Vector3(textMesh.preferredWidth + Margin, textMesh.preferredHeight + Margin, background.transform.localScale.z);
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
            // �ؽ�Ʈ ����
            textMesh.text = InsertCharacterBasedLineBreaks(BdObject.Name, option.lineLength);
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
                option.align switch
                {
                    "center" => TextAlignmentOptions.Bottom,
                    "right" => TextAlignmentOptions.BottomRight,
                    _ => TextAlignmentOptions.BottomLeft
                };

            textMesh.transform.localPosition = new Vector3(0, textMesh.preferredHeight / 2.0f, 0);
        }

        private static string InsertCharacterBasedLineBreaks(string text, float maxWidth)
        {
            var sb = new StringBuilder();
            var add = 1f;
            if (maxWidth >= 7)
            {
                add = 0.5f;
            }
            var count = 0f;

            foreach (var c in text)
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
                    sb.Append("\n"); // �ٹٲ� ����
                    count = 0f;
                }
            }

            return sb.ToString();
        }

        public static Color HexToColor(string hex)
        {
            // Unity�� ColorUtility.TryParseHtmlString()�� ����Ͽ� ��ȯ
            if (ColorUtility.TryParseHtmlString(hex, out var color))
            {
                return color;
            }

            Debug.LogError("Invalid HEX Color: " + hex);
            return Color.white; // �⺻������ White ��ȯ
        }
    }
}
