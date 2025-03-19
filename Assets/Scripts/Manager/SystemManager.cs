//using B83.Win32;

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BDObjectSystem;

namespace Manager
{
    public class SystemManager : BaseManager
    {
        public string[] filesDropped;


        private float _deltaTime;

        [SerializeField] private int size = 15;
        [SerializeField] private Color color = Color.white;

        private GUIStyle _style;

        protected override void Awake()
        {
            base.Awake();

            Application.targetFrameRate = 165;

            //UnityDragAndDropHook.InstallHook();
            //UnityDragAndDropHook.OnDroppedFiles += OnFiles;
        }

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
        }

        private void OnGUI()
        {


            var rect = new Rect(Screen.width - 200, 30, Screen.width, Screen.height);

            var ms = _deltaTime * 1000f;
            var fps = 1.0f / _deltaTime;
            var text = $"{fps:0.} FPS ({ms:0.0} ms)";
        
            var versionRect = new Rect(Screen.width - 200, 10, Screen.width, Screen.height);
            var version = string.Format("Version: {0}", Application.version);

            GUI.Label(rect, text, _style);
            GUI.Label(versionRect, version, _style);
        }

        //private void OnDestroy()
        //{
        //    UnityDragAndDropHook.UninstallHook();
        //}

        //public void OnFiles(List<string> aPathNames, POINT aDropPoint)
        //{
        //    GameManager.GetManager<FileManager>().AfterLoadFile(aPathNames.ToArray());
        //}

        // Update is called once per frame
        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;

            // paste from clipboard
            if (!Input.GetKey(KeyCode.LeftControl) || !Input.GetKeyDown(KeyCode.V)) return;
            try
            {
                var clipboard = GUIUtility.systemCopyBuffer;

                CustomLog.Log("Clipboard: " + clipboard);

                _ = GameManager.GetManager<FileManager>().MakeDisplay(clipboard);
            }
            catch (Exception e)
            {
                CustomLog.Log("Clipboard is not BDEFile");
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
            }
        }
        
        public void ExportPrefab(int idx)
        {
            var bdObjParent = GameManager.GetManager<BdObjectManager>().bdObjectParent;
            if (idx >= bdObjParent.childCount) return;
            
            var prefab = bdObjParent.GetChild(idx).gameObject;
            
            if (!prefab)
            {
                Debug.LogError("Target object is null. Assign a GameObject.");
                return;
            }

            const string directory = "Assets/RuntimePrefabs";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var prefabPath = $"{directory}/{prefab.name}.prefab";

            // 프리팹 저장
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
    }
}
