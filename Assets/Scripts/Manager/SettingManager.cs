using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : BaseManager
{
    public int DefaultInterpolation = 0;
    public int DefaultTickInterval = 5;

    public bool UseFindMode = true;
    public string FakePlayer = "anim";
    public string ScoreboardName = "anim";
    public int StartTick = 0;
    public string Namespace = "PotanAnim";
    public string FrameFileName = "frame";
    public string FrameFilePath = "result";
    public string ResultPath = "result";

    public Toggle FindModeToggle;
    public TMP_InputField[] inputFields;
    public GameObject SettingPanel;

    private void Start()
    {
        FindModeToggle.onValueChanged.AddListener(
            (value) =>
            {
                UseFindMode = value;
                if (!UseFindMode)
                    inputFields[2].text = "@s";
                });
        for (int i = 0; i < inputFields.Length; i++)
        {
            int idx = i;
            inputFields[i].onEndEdit.AddListener((value) => OnEndEditValue(value, idx));
            OnEndEditValue(inputFields[i].text, i);
        }
    }

    public void SetSettingPanel(bool IsOn)
    {
        SettingPanel.SetActive(IsOn);
    }

    void OnEndEditValue(string value, int idx)
    {
        //Debug.Log(idx);
        switch (idx)
        {
            case 0:
                if (int.TryParse(value, out int tickInterval) && tickInterval >= 0)
                {
                    DefaultTickInterval = tickInterval;
                }
                else
                {
                    value = DefaultTickInterval.ToString();
                }
                break;
            case 1:
                if (int.TryParse(value, out int Interpolation) && Interpolation >= 0)
                {
                    DefaultInterpolation = Interpolation;
                }
                else
                {
                    value = Interpolation.ToString();
                }
                break;
            case 2:
                if (!UseFindMode)
                    value = "@s";
                else
                    FakePlayer = value;
                break;
            case 3:
                ScoreboardName = value;
                break;
            case 4:
                if (int.TryParse(value, out int startTick) && startTick >= 0)
                    StartTick = startTick;
                else
                    value = StartTick.ToString();
                break;
            case 5:
                Namespace = value;
                break;
            case 6:
                FrameFileName = value;
                break;
            case 7:
                FrameFilePath = value;
                break;
            case 8:
                ResultPath = value;
                break;
        }
        inputFields[idx].text = value;
    }
}
