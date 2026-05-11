//using B83.Win32;

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDObjectSystem;
using Minecraft;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using FileSystem;

namespace GameSystem
{
    public class SystemManager : BaseManager
    {
        public List<string> filesDropped;

        private WindowFileHandler _fileDragAndDrop; // FildDragAndDrop 참조 저장

        private float _deltaTime;

        [SerializeField] private int size = 15;
        [SerializeField] private Color color = Color.white;

        private GUIStyle _style;

        private FileLoadManager fileLoadManager;

        // protected override void Awake()
        // {
        //     base.Awake();

        //     // QualitySettings.vSyncCount = 1;
        // }

        private void Start()
        {
            _style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = size,
                normal =
                {
                    textColor = color
                }
            };

            if (MinecraftFileManager.Instance.IsReadedFiles == false)
            {
                SceneManager.LoadScene("Mainmenu");
            }

            _fileDragAndDrop = GetComponent<WindowFileHandler>(); // 컴포넌트 참조 가져오기
            _fileDragAndDrop.OnFilesDropped.AddListener(OnFilesDropped);

            fileLoadManager = GameManager.GetManager<FileLoadManager>();

        }

        private void OnGUI()
        {
            var rect = new Rect(Screen.width - 200, 100, Screen.width, Screen.height);

            var ms = _deltaTime * 1000f;
            var fps = 1.0f / _deltaTime;
            var text = $"{fps:0.} FPS ({ms:0.0} ms)";

            var versionRect = new Rect(Screen.width - 200, 70, Screen.width, Screen.height);
            var version = string.Format("Version: {0}", Application.version);

            GUI.Label(rect, text, _style);
            GUI.Label(versionRect, version, _style);
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        private void OnFilesDropped(List<string> files)
        {
            if (files == null || files.Count == 0)
            {
                return;
            }

            CustomLog.Log($"{files.Count}개의 파일을 인식했습니다. 1번: {files[0]}");
            foreach (var file in files)
            {
                // 1. Path.GetExtension으로 파일의 실제 확장자를 가져옵니다. (예: ".mdeanim")
                string extension = Path.GetExtension(file);

                // 2. 대소문자를 무시하고 안전하게 비교합니다.
                // SaveManager.MDEFileExtension에 "."이 없으므로, 비교할 때 추가해줍니다.
                if (string.Equals(extension, "." + SaveManager.MDEFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    var save = GameManager.GetManager<SaveManager>();
                    save.LoadMCDEFileFromPath(file).Forget();
                    return;
                }
            }
            // FileLoadManager가 파일/폴더 처리 로직을 모두 담당하도록 전달합니다.
            fileLoadManager.FileDroped(files);
        }

        public void OnCopyKey(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                CustomLog.Log("Copy key pressed");
                // 여기에 복사 로직을 추가할 수 있습니다.
            }
        }

        public void OnPasteKey(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                var clipboardText = GUIUtility.systemCopyBuffer;
                CustomLog.Log("Paste key pressed");
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    CustomLog.Log($"Clipboard text: {clipboardText}");
                }
                else
                {
                    _fileDragAndDrop.CheckForPastedFiles();

                }
                // Debug.Log($"Pasted files count: {pastedFiles}");

                // if (pastedFiles != null && pastedFiles.Count > 0)
                // {
                //     // 파일이 붙여넣기 된 경우, OnFilesDropped를 호출하여 동일한 로직으로 처리
                //     CustomLog.Log($"Pasted files detected via clipboard: {pastedFiles.Count} file(s).");
                //     OnFilesDropped(pastedFiles);
                // }
                // else
                // {
                //     // 파일이 아닌 경우, 기존의 텍스트 클립보드 로직 수행
                //     var clipboardText = GUIUtility.systemCopyBuffer;
                //     if (!string.IsNullOrEmpty(clipboardText))
                //     {
                //         CustomLog.Log($"Paste key pressed. Clipboard text: {clipboardText}");
                //     }
                //     else
                //     {
                //         CustomLog.Log("Paste key pressed, but clipboard is empty or contains no files.");
                //     }
                // }
            }
        }
    }
}
