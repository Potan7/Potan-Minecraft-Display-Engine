using System;
using Animation;
using FileSystem;
using GameSystem;
using TMPro;
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

        [SerializeField]
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
                PlayerPrefs.Save(); // 변경 사항 저장
            }
        }
        [SerializeField]
        private bool _useFxSort = true;
        /// <summary>
        /// 프레임 txt 파일 사용 여부
        /// </summary>
        public bool UseFxSort
        {
            get => _useFxSort;
            set
            {
                _useFxSort = value;
                PlayerPrefs.SetInt("useFxSort", value ? 1 : 0);
                PlayerPrefs.Save(); // 변경 사항 저장

                toggleButtons[(int)SettingToggleType.UseNameInfoExtract].interactable = value;
                if (!value)
                {
                    toggleButtons[(int)SettingToggleType.UseNameInfoExtract].isOn = false;
                    UseNameInfoExtract = false;
                }
            }
        }

        /// <summary>
        /// 생성 모드
        /// </summary>
        // public bool useFindMode = true;

        // public string fakePlayer = "anim";
        // public string scoreboardName = "anim";

        /// <summary>
        /// 내보냈을 때 최초 틱 
        /// </summary>
        // public int startTick;
        /// <summary>
        /// 내보냈을 때 데이터팩 네임스페이스 
        /// </summary>
        // public string packNamespace = "PotanAnim:anim/";
        /// <summary>
        /// 프레임 파일들의 이름
        /// </summary>
        // public string frameFileName = "frame";
        /// <summary>
        /// 1초당 틱 수 
        /// </summary>
        public int tickUnit = 10;

        // [FormerlySerializedAs("FindModeToggle")] public Toggle findModeToggle;
        public TMP_InputField[] inputFields;
        [FormerlySerializedAs("SettingPanel")] public GameObject settingPanel;

        // public ExportManager exportManager; // 직접 참조 대신 GameManager 통해 접근하거나, ExportSettingUIManager가 관리
        public BdEngineStyleCameraMovement cameraMovement;

        public Slider[] sliders;
        public Toggle[] toggleButtons; // 배열 크기 및 인덱스 재조정 필요

        // InputField 인덱스를 위한 Enum (권장)
        enum SettingInputFieldType
        {
            DefaultTickInterval, // 0
            DefaultInterpolation, // 1
            // FakePlayer, // 제거 (ExportSettingUI)
            // ScoreboardName, // 제거
            // StartTick, // 제거
            // PackNamespace, // 제거
            // FrameFileName, // 제거
            // ExportFolder, // 제거
            TickUnit // 이전 인덱스 8에서 2로 변경될 수 있음
        }

        enum SettingToggleType
        {
            UseNameInfoExtract, // 0
            UseFxSort, // 1
            // FindMode // 제거 (ExportSettingUI)
        }


        enum SliderType
        {
            CameraSpeed,
            CameraMoveSpeed,
            CameraZoomSpeed,
        }

        #endregion

        private void Start()
        {
            // --- findModeToggle 리스너 제거 ---
            // findModeToggle.onValueChanged.AddListener(...);

            // PlayerPrefs에서 값 불러오기 (기존 로직 유지)
            defaultTickInterval = PlayerPrefs.GetInt("defaultTickInterval", defaultTickInterval);
            defaultInterpolation = PlayerPrefs.GetInt("defaultInterpolation", defaultInterpolation);
            UseNameInfoExtract = PlayerPrefs.GetInt("useNameInfoExtract", UseNameInfoExtract ? 1 : 0) == 1;
            UseFxSort = PlayerPrefs.GetInt("useFrameTxtFile", UseFxSort ? 1 : 0) == 1;
            tickUnit = PlayerPrefs.GetInt("tickUnit", tickUnit);
            GameManager.GetManager<AnimManager>().TickUnit = 1.0f / tickUnit;

            cameraMovement.rotateSensitivity = PlayerPrefs.GetFloat("cameraSpeed", cameraMovement.rotateSensitivity);
            cameraMovement.panSensitivity = PlayerPrefs.GetFloat("cameraMoveSpeed", cameraMovement.panSensitivity);
            cameraMovement.zoomSensitivity = PlayerPrefs.GetFloat("cameraZoomSpeed", cameraMovement.zoomSensitivity);

            sliders[(int)SliderType.CameraSpeed].value = cameraMovement.rotateSensitivity;
            sliders[(int)SliderType.CameraMoveSpeed].value = cameraMovement.panSensitivity;
            sliders[(int)SliderType.CameraZoomSpeed].value = cameraMovement.zoomSensitivity;

            // inputFields에도 불러온 값 반영 (인덱스 수정 필요)
            inputFields[(int)SettingInputFieldType.DefaultTickInterval].text = defaultTickInterval.ToString();
            inputFields[(int)SettingInputFieldType.DefaultInterpolation].text = defaultInterpolation.ToString();
            inputFields[(int)SettingInputFieldType.TickUnit].text = tickUnit.ToString();
            // --- Export 관련 inputFields 반영 제거 ---

            toggleButtons[(int)SettingToggleType.UseNameInfoExtract].isOn = UseNameInfoExtract;
            toggleButtons[(int)SettingToggleType.UseFxSort].isOn = UseFxSort;
            // --- findModeToggle 반영 제거 ---


            for (var i = 0; i < inputFields.Length; i++)
            {
                // inputFields 배열이 Export 관련 필드를 제외하고 재구성되었다고 가정
                // 또는 Enum을 사용하여 정확한 필드에 리스너 연결
                int idx = i; // 이 인덱스는 SettingInputFieldType enum 값과 일치해야 함
                inputFields[i].onEndEdit.AddListener(value => OnEndEditValue(value, (SettingInputFieldType)idx));
            }

            for (var i = 0; i < sliders.Length; i++)
            {
                int idx = i;
                sliders[i].onValueChanged.AddListener(value => OnSliderValueEdited(value, (SliderType)idx));
            }
        }

        public void SetSettingPanel(bool isOn)
        {
            settingPanel.SetActive(isOn);
            if (isOn)
            {
                // UIManager.CurrentUIStatus |= UIManager.UIStatus.OnSettingPanel;
                UIManager.SetUIStatus(UIManager.UIStatus.OnSettingPanel, true);
            }
            else
            {
                // UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnSettingPanel;
                UIManager.SetUIStatus(UIManager.UIStatus.OnSettingPanel, false);
            }
        }

        // OnEndEditValue의 idx는 SettingInputFieldType enum을 사용하도록 변경
        private void OnEndEditValue(string value, SettingInputFieldType type)
        {
            switch (type)
            {
                case SettingInputFieldType.DefaultTickInterval:
                    if (int.TryParse(value, out var tickInterval) && tickInterval >= 1)
                    {
                        defaultTickInterval = tickInterval;
                        PlayerPrefs.SetInt("defaultTickInterval", defaultTickInterval);
                    }
                    else
                    {
                        inputFields[(int)type].text = defaultTickInterval.ToString(); // 수정된 부분: inputFields[idx] 대신 inputFields[(int)type]
                    }
                    break;
                case SettingInputFieldType.DefaultInterpolation:
                    if (int.TryParse(value, out var interpolation) && interpolation >= 0)
                    {
                        defaultInterpolation = interpolation;
                        PlayerPrefs.SetInt("defaultInterpolation", defaultInterpolation);
                    }
                    else
                    {
                        inputFields[(int)type].text = defaultInterpolation.ToString(); // 수정된 부분
                    }
                    break;
                case SettingInputFieldType.TickUnit:
                    if (int.TryParse(value, out var unit) && unit >= 1)
                    {
                        tickUnit = unit;
                        GameManager.GetManager<AnimManager>().TickUnit = 1.0f / tickUnit;
                        PlayerPrefs.SetInt("tickUnit", tickUnit);
                    }
                    else
                    {
                        inputFields[(int)type].text = tickUnit.ToString(); // 수정된 부분
                    }
                    break;
                // --- Export 관련 case 제거 ---
            }
            // inputFields[idx].text = value; // 이 줄은 각 case 내부에서 처리하거나, 마지막에 한 번만 호출 (값이 변경되지 않았을 경우 대비)
        }


        private void OnSliderValueEdited(float value, SliderType type)
        {
            // ... (기존 슬라이더 로직 유지) ...
            switch (type)
            {
                case SliderType.CameraSpeed:
                    cameraMovement.rotateSensitivity = value;
                    PlayerPrefs.SetFloat("cameraSpeed", value);
                    break;
                case SliderType.CameraMoveSpeed:
                    cameraMovement.panSensitivity = value;
                    PlayerPrefs.SetFloat("cameraMoveSpeed", value);
                    break;
                case SliderType.CameraZoomSpeed:
                    cameraMovement.zoomSensitivity = value;
                    PlayerPrefs.SetFloat("cameraZoomSpeed", value);
                    break;
            }
        }
    }
}
