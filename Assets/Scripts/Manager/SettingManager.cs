using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Manager
{
    public class SettingManager : BaseManager
    {
        [FormerlySerializedAs("DefaultInterpolation")] public int defaultInterpolation;
        [FormerlySerializedAs("DefaultTickInterval")] public int defaultTickInterval = 5;

        public bool UseNameInfoExtract { get; set; } = true;
        public bool UseFrameTxtFile { get; set; } = true;

        [FormerlySerializedAs("UseFindMode")] public bool useFindMode = true;
        [FormerlySerializedAs("FakePlayer")] public string fakePlayer = "anim";
        [FormerlySerializedAs("ScoreboardName")] public string scoreboardName = "anim";
        [FormerlySerializedAs("StartTick")] public int startTick;
        [FormerlySerializedAs("Namespace")] public string @namespace = "PotanAnim";
        [FormerlySerializedAs("FrameFileName")] public string frameFileName = "frame";
        [FormerlySerializedAs("FrameFilePath")] public string frameFilePath = "result";
        [FormerlySerializedAs("ResultPath")] public string resultPath = "result";

        [FormerlySerializedAs("FindModeToggle")] public Toggle findModeToggle;
        public TMP_InputField[] inputFields;
        [FormerlySerializedAs("SettingPanel")] public GameObject settingPanel;

        private void Start()
        {
            findModeToggle.onValueChanged.AddListener(
                value =>
                {
                    useFindMode = value;
                    if (!useFindMode)
                        inputFields[2].text = "@s";
                });
            for (var i = 0; i < inputFields.Length; i++)
            {
                var idx = i;
                inputFields[i].onEndEdit.AddListener(value => OnEndEditValue(value, idx));
                OnEndEditValue(inputFields[i].text, i);
            }
        }

        public void SetSettingPanel(bool isOn)
        {
            settingPanel.SetActive(isOn);
        }

        private void OnEndEditValue(string value, int idx)
        {
            //Debug.Log(idx);
            switch (idx)
            {
                case 0:
                    if (int.TryParse(value, out var tickInterval) && tickInterval >= 1)
                    {
                        defaultTickInterval = tickInterval;
                    }
                    else
                    {
                        value = defaultTickInterval.ToString();
                    }
                    break;
                case 1:
                    if (int.TryParse(value, out var interpolation) && interpolation >= 0)
                    {
                        defaultInterpolation = interpolation;
                    }
                    else
                    {
                        value = interpolation.ToString();
                    }
                    break;
                case 2:
                    if (!useFindMode)
                        value = "@s";
                    else
                        fakePlayer = value;
                    break;
                case 3:
                    scoreboardName = value;
                    break;
                case 4:
                    if (int.TryParse(value, out var tick) && tick >= 0)
                        startTick = tick;
                    else
                        value = startTick.ToString();
                    break;
                case 5:
                    @namespace = value;
                    break;
                case 6:
                    frameFileName = value;
                    break;
                case 7:
                    frameFilePath = value;
                    break;
                case 8:
                    resultPath = value;
                    break;
            }
            inputFields[idx].text = value;
        }
    }
}
