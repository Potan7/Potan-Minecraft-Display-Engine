using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using FileSystem;
using System;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Mainmenu;
using DG.Tweening;

namespace GameSystem
{
    public class UIManager : BaseManager
    {

        [Flags]
        public enum UIStatus
        {
            None = 0,
            OnAnimUIPanel = 1 << 0,
            OnSettingPanel = 1 << 1,
            OnExportPanel = 1 << 2,
            OnDraggingPanel = 1 << 3,
            OnMenuBarPanel = 1 << 4,
            OnPopupPanel = 1 << 5,
        }

        public static UIStatus CurrentUIStatus { get; private set; } = UIStatus.None;
        public static void SetUIStatus(UIStatus status, bool isOn = true)
        {
            if (isOn)
            {
                CurrentUIStatus |= status;
            }
            else
            {
                CurrentUIStatus &= ~status;
            }

            bool checkPanelStatus = (CurrentUIStatus & (UIStatus.OnExportPanel | UIStatus.OnPopupPanel | UIStatus.OnSettingPanel)) == 0;
            GameManager.SetPlayerInput(checkPanelStatus);

            GameManager.GetManager<BdEngineStyleCameraMovement>().enableCameraMovement = (CurrentUIStatus == UIStatus.None);
            
        }

        const string DefaultLoadingText = "Loading...";

        private FileLoadManager _fileManager;
        public PopupPanelManager popupManager;

        [FormerlySerializedAs("LoadingPanel")] public GameObject loadingPanel;
        public TextMeshProUGUI loadingText;

        public CanvasGroup canvasGroup;

        private void Start()
        {
            _fileManager = GameManager.GetManager<FileLoadManager>();

            CurrentUIStatus = UIStatus.None;

            canvasGroup = GetComponent<CanvasGroup>();

            if (MainmenuManager.isFirstVisiting)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;

                canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.InQuart).OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                });
            }
        }

        public void SetLoadingPanel(bool isOn)
        {
            loadingPanel.SetActive(isOn);
            if (isOn)
            {
                CursorManager.SetCursor(CursorManager.CursorType.Loading);
            }
            else
            {
                CursorManager.SetCursor(CursorManager.CursorType.Default);
                loadingText.text = DefaultLoadingText;
            }
        }

        public void SetLoadingText(string text)
        {
            loadingText.text = text;
        }

        public void OnPressImportButton()
        {
            _fileManager.ImportFile();
        }

        public void ShowPopupPanel(string text1, string text2, Action<bool> OnApplyOrCancel = null)
        {
            // CurrentUIStatus |= UIStatus.OnPopupPanel;
            // SetUIStatus(UIStatus.OnPopupPanel, true);

            // 콜백을 래핑하여 UI 상태를 다시 해제하는 로직 추가
            // Action<bool> wrappedCallback = isApply =>
            // {
            //     CurrentUIStatus &= ~UIStatus.OnPopupPanel;
            //     OnApplyOrCancel?.Invoke(isApply);
            // };

            popupManager.ShowPopup(text1, text2, OnApplyOrCancel);
        }

        /// <summary>
        /// 팝업을 비동기적으로 표시하고 사용자의 선택(true/false)을 반환합니다.
        /// </summary>
        public async UniTask<bool> ShowPopupPanelAsync(string text1, string text2)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            popupManager.ShowPopup(text1, text2, isApply =>
            {
                tcs.TrySetResult(isApply);
            });
            return await tcs.Task;
        }

        void OnDestroy()
        {
            CurrentUIStatus = UIStatus.None;
            
        }
    }
}
