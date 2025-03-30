using System;
using Animation;
using GameSystem;
using TMPro;
using FileSystem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GameSystem
{
    public class SettingManager : BaseManager
    {
        #region 변수
        /// <summary>
        /// 프레임 기본 틱 간격
        /// </summary>
        public int defaultInterpolation;

        /// <summary>
        /// 프레임 기본 보간 값
        /// </summary>
        public int defaultTickInterval;

        private bool _useNameInfoExtract = true;
        /// <summary>
        /// 이름 정보 추출 사용 여부
        /// </summary>
        public bool UseNameInfoExtract
        {
            get => _useNameInfoExtract;
            set
            {
                _useNameInfoExtract = value;
                PlayerPrefs.SetInt("useNameInfoExtract", value ? 1 : 0);
            }
        }
        private bool _useFrameTxtFile = true;
        /// <summary>
        /// 프레임 txt 파일 사용 여부
        /// </summary>
        public bool UseFrameTxtFile
        {
            get => _useFrameTxtFile;
            set
            {
                _useFrameTxtFile = value;
                PlayerPrefs.SetInt("useFrameTxtFile", value ? 1 : 0);
            }
        }

        /// <summary>
        /// 생성 모드
        /// </summary>
        public bool useFindMode = true;

        public string fakePlayer = "anim";
        public string scoreboardName = "anim";

        /// <summary>
        /// 내보냈을 때 최초 틱 
        /// </summary>
        public int startTick;
        /// <summary>
        /// 내보냈을 때 데이터팩 네임스페이스 
        /// </summary>
        public string packNamespace = "PotanAnim";
        /// <summary>
        /// 프레임 파일들의 이름
        /// </summary>
        public string frameFileName = "frame";
        /// <summary>
        /// 프레임 파일들이 저장될 경로 
        /// </summary>
        public string frameFilePath = "result";
        /// <summary>
        /// 1초당 틱 수 
        /// </summary>
        public int tickUnit = 10;

        [FormerlySerializedAs("FindModeToggle")] public Toggle findModeToggle;
        public TMP_InputField[] inputFields;
        [FormerlySerializedAs("SettingPanel")] public GameObject settingPanel;

        public ExportManager exportManager;
        public BdEngineStyleCameraMovement cameraMovement;

        public Slider[] sliders;

        enum SliderType
        {
            CameraSpeed,
            CameraMoveSpeed,
            CameraZoomSpeed,
        }
        #endregion

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

            if (PlayerPrefs.HasKey("defaultInterpolation"))
            {
                defaultInterpolation = PlayerPrefs.GetInt("defaultInterpolation");
            }
            if (PlayerPrefs.HasKey("defaultTickInterval"))
            {
                defaultTickInterval = PlayerPrefs.GetInt("defaultTickInterval");
            }
            if (PlayerPrefs.HasKey("useNameInfoExtract"))
            {
                UseNameInfoExtract = PlayerPrefs.GetInt("useNameInfoExtract") == 1;
            }
            if (PlayerPrefs.HasKey("useFrameTxtFile"))
            {
                UseFrameTxtFile = PlayerPrefs.GetInt("useFrameTxtFile") == 1;
            }
            if (PlayerPrefs.HasKey("tickUnit"))
            {
                tickUnit = PlayerPrefs.GetInt("tickUnit");
                GameManager.GetManager<AnimManager>().TickUnit = 1.0f / tickUnit;
            }
            if (PlayerPrefs.HasKey("cameraSpeed"))
            {
                cameraMovement.rotateSpeed = PlayerPrefs.GetFloat("cameraSpeed");
                sliders[(int)SliderType.CameraSpeed].value = cameraMovement.rotateSpeed;
            }
            if (PlayerPrefs.HasKey("cameraMoveSpeed"))
            {
                cameraMovement.panSpeed = PlayerPrefs.GetFloat("cameraMoveSpeed");
                sliders[(int)SliderType.CameraMoveSpeed].value = cameraMovement.panSpeed;
            }
            if (PlayerPrefs.HasKey("cameraZoomSpeed"))
            {
                cameraMovement.zoomSpeed = PlayerPrefs.GetFloat("cameraZoomSpeed");
                sliders[(int)SliderType.CameraZoomSpeed].value = cameraMovement.zoomSpeed;
            }
            
            for (var i = 0; i < sliders.Length; i++)
            {
                var idx = i;
                sliders[i].onValueChanged.AddListener(value => OnSliderValueEdited(value, (SliderType)idx));
            }
        }

        public void SetSettingPanel(bool isOn)
        {
            settingPanel.SetActive(isOn);
            if (isOn)
            {
                UIManager.CurrentUIStatus |= UIManager.UIStatus.OnSettingPanel;
            }
            else
            {
                UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnSettingPanel;
            }
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
                        PlayerPrefs.SetInt("defaultTickInterval", defaultTickInterval);
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
                        PlayerPrefs.SetInt("defaultInterpolation", defaultInterpolation);
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
                    packNamespace = value;
                    break;
                case 6:
                    frameFileName = value;
                    break;
                case 7:
                    exportManager.ExportPath = value;
                    break;
                case 8:
                    if (int.TryParse(value, out var unit) && unit >= 1)
                    {
                        tickUnit = unit;
                        GameManager.GetManager<AnimManager>().TickUnit = 1.0f / tickUnit;
                        PlayerPrefs.SetInt("tickUnit", tickUnit);
                    }
                    else
                        value = tickUnit.ToString();
                    break;
            }
            inputFields[idx].text = value;
        }

        private void OnSliderValueEdited(float value, SliderType type)
        {
            switch (type)
            {
                case SliderType.CameraSpeed:
                    cameraMovement.rotateSpeed = value;
                    PlayerPrefs.SetFloat("cameraSpeed", value);
                    break;
                case SliderType.CameraMoveSpeed:
                    cameraMovement.panSpeed = value;
                    PlayerPrefs.SetFloat("cameraMoveSpeed", value);
                    break;
                case SliderType.CameraZoomSpeed:
                    cameraMovement.zoomSpeed = value;
                    PlayerPrefs.SetFloat("cameraZoomSpeed", value);
                    break;
            }
        }
    }
}
