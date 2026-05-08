using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using FileSystem;
using LitMotion;
using LitMotion.Extensions;
using Newtonsoft.Json;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace BDObjectSystem
{
    public class TagUUIDAdder : MonoBehaviour
    {
        public static string LauncherPath;

        readonly string[] TEXTLIST =
        {
            "설정될 태그의 이름을 적어주세요",
            "설정될 UUID의 시작 숫자를 적어주세요",
            "설정될 태그는 다음과 같습니다.",
            "설정될 UUID는 다음과 같습니다.",
            "모든 디스플레이에 {0}0 태그,\n각 디스플레이에 {0}(숫자) 태그가 들어갑니다.",
            "모든 디스플레이에 {0}0 태그,\n각 디스플레이에 UUID:[I;{1},(숫자),0,0]이 들어갑니다.",
        };

        // readonly FileBrowser.Filter loadFilter = new FileBrowser.Filter("Files", ".bdengine", ".bdstudio");

        /// <summary>
        /// 0 - 설정될 태그의 이름을 ~
        /// 1 - 설정될 태그는 다음과 ~
        /// 2 - 각 디스플레이 ~
        /// 3 - File Name
        /// </summary>
        public TextMeshProUGUI[] infoTexts;

        public enum ADDTYPE
        {
            TAG,
            UUID,
        }

        ADDTYPE addType;
        public ADDTYPE AddType
        {
            get { return addType; }
            set
            {
                addType = value;
                infoTexts[0].text = TEXTLIST[addType == ADDTYPE.TAG ? 0 : 1];
                infoTexts[1].text = TEXTLIST[addType == ADDTYPE.TAG ? 2 : 3];

                switch (addType)
                {
                    case ADDTYPE.TAG:
                        uuidStartNumberInputField.gameObject.SetActive(false);
                        break;
                    case ADDTYPE.UUID:
                        uuidStartNumberInputField.gameObject.SetActive(true);
                        break;
                }
                UpdateInfoText();
            }
        }

        [SerializeField]
        private string tagName;
        public string TagName
        {
            get { return tagName; }
            set
            {
                tagName = value;

                UpdateInfoText();
                CheckSaveButton();
            }
        }

        public int uuidStartNumber;
        public string UUIDStartNumber
        {
            get { return uuidStartNumber.ToString(); }
            set
            {
                if (!int.TryParse(value, out int result))
                {
                    uuidStartNumber = 0;
                    uuidStartNumberInputField.text = UUIDStartNumber;
                }
                else
                {
                    uuidStartNumber = result;
                }

                UpdateInfoText();
                CheckSaveButton();
            }
        }

        public string filePath;

        public Toggle[] addTypeToggles;

        public Button saveFileButton;

        public bool IsReplacingTag { get; set; } = false;

        public TMP_InputField tagNameInputField;
        public TMP_InputField uuidStartNumberInputField;

        public GameObject loadingPanel;

        public event Action<BdObject> OnBDObjectEdited;

        Queue<BdObject> queue = new Queue<BdObject>();

        public Toggle IsReplacingTagToggle;

        public RectTransform panel;

        public RectTransform tagAdderPanel;

        float panelHiddenY;


        void Start()
        {
            LauncherPath = Application.dataPath + "/../";

            addTypeToggles[0].onValueChanged.AddListener((_) => { AddType = ADDTYPE.TAG; });
            addTypeToggles[1].onValueChanged.AddListener((_) => { AddType = ADDTYPE.UUID; });



            IsReplacingTagToggle.onValueChanged.AddListener((isOn) =>
            {
                IsReplacingTag = isOn;
            });

            panelHiddenY = -panel.rect.height * 2f;
            panel.localPosition = new Vector3(panel.localPosition.x, panelHiddenY, panel.localPosition.z);

            tagAdderPanel.gameObject.SetActive(false);
        }

        public void OnAddFileButton()
        {
            var path = StandaloneFileBrowser.OpenFilePanel("Select File",
                LauncherPath,
                FileLoadManager.extension
                , false);

            if (path.Length > 0)
            {
                SetFilePath(path[0]);
            }
            // await AddFileCoroutine();
        }

        public void SetFilePath(string path)
        {
            filePath = path;
            var fileName = Path.GetFileName(filePath);
            infoTexts[3].text = fileName;
            CheckSaveButton();
        }

        public void SetPanelActive(bool active)
        {
            if (active)
            {
                tagAdderPanel.gameObject.SetActive(true);

                LMotion.Create(panelHiddenY, 0, 0.5f)
                    .WithEase(Ease.OutBack)
                    .BindToAnchoredPositionY(panel);
            }
            else
            {
                LMotion.Create(panel.anchoredPosition.y, panelHiddenY, 0.5f)
                    .WithEase(Ease.InQuad)
                    .WithOnComplete(() => tagAdderPanel.gameObject.SetActive(false))
                    .BindToAnchoredPositionY(panel);
            }

            IsReplacingTagToggle.isOn = IsReplacingTag;
            addTypeToggles[0].isOn = AddType == ADDTYPE.TAG;
        }

        void CheckSaveButton()
        {
            // 버튼 활성화 조건: 태그 이름과 파일 경로가 모두 입력된 경우 + 만약 UUID일 경우 시작 숫자도 입력된 경우
            bool isTagNameValid = !string.IsNullOrEmpty(tagName);
            bool isFilePathValid = !string.IsNullOrEmpty(filePath);

            saveFileButton.interactable = isTagNameValid && isFilePathValid;
        }

        void UpdateInfoText()
        {
            var infoText = infoTexts[2];
            if (string.IsNullOrEmpty(tagName))
            {
                infoText.text = string.Empty;
            }
            else if (AddType == ADDTYPE.TAG)
            {
                infoText.text = string.Format(TEXTLIST[4], tagName);
            }
            else if (AddType == ADDTYPE.UUID)
            {
                infoText.text = string.Format(TEXTLIST[5], tagName, uuidStartNumber);
            }
        }

        public async void OnSaveFileButton()
        {
            try
            {
                // 1. File Load
                var bdobject = await FileProcessingHelper.ProcessFileAsync(filePath);
                if (bdobject == null)
                {
                    CustomLog.LogError("파일을 로드하지 못했습니다.");
                    return;
                }
                await ApplyTagOrUUID(bdobject);
            }
            catch (Exception e)
            {
                CustomLog.UnityLog(e.Message);
                loadingPanel.SetActive(false);
            }
        }

        public async UniTask ApplyTagOrUUID(BdObject bdobject, bool SaveFile = true)
        {
            loadingPanel.SetActive(true);

            await UniTask.SwitchToTaskPool();

            // Tag or UUID Apply

            queue.Clear();
            queue.Enqueue(bdobject);

            int idx = 1;

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();

                if (obj.IsDisplay)
                {
                    var tag = tagName + "0";

                    if (AddType == ADDTYPE.UUID)
                    {
                        var uuidToAdd = $"UUID:[I;{uuidStartNumber},{idx++},0,0]";
                        const string uuidPattern = @"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]";
                        var matchUUID = Regex.Match(obj.Nbt, uuidPattern);

                        if (matchUUID.Success)
                        {
                            // 기존 UUID 블록이 있으면 대체
                            string existingUuids = matchUUID.Value;
                            obj.Data.nbt = obj.Nbt.Replace(existingUuids, uuidToAdd);
                        }
                        else
                        {
                            // 기존 UUID 블록이 없으면 새로 생성
                            obj.Data.nbt = string.IsNullOrEmpty(obj.Nbt) ? uuidToAdd : $"{obj.Nbt},{uuidToAdd}";
                        }
                    }
                    else if (AddType == ADDTYPE.TAG)
                    {
                        tag += $",{tagName}{idx++}";
                    }

                    const string tagPattern = @"Tags:\[([^\]]*)\]";
                    var match = Regex.Match(obj.Nbt, tagPattern);

                    if (IsReplacingTag)
                    {
                        // 기존 Tags 블록을 새 태그로 대체 (대괄호 유지)
                        obj.Data.nbt = Regex.Replace(obj.Nbt, tagPattern, $"Tags:[{tag}]");
                        if (!match.Success && !obj.Nbt.Contains($"Tags:[{tag}]")) // 기존에 없었고 대체도 못했으면 추가
                        {
                            obj.Data.nbt = string.IsNullOrEmpty(obj.Nbt) ? $"Tags:[{tag}]" : $"{obj.Nbt},Tags:[{tag}]";
                        }
                    }
                    else
                    {
                        if (match.Success)
                        {
                            // 기존 Tags 블록이 있으면 내부 태그 목록에 추가
                            string existingTags = match.Groups[1].Value;
                            string newTags = string.IsNullOrEmpty(existingTags) ? tag : $"{existingTags},{tag}";
                            obj.Data.nbt = Regex.Replace(obj.Nbt, tagPattern, $"Tags:[{newTags}]");
                        }
                        else
                        {
                            // 기존 Tags 블록이 없으면 새로 생성
                            obj.Data.nbt = string.IsNullOrEmpty(obj.Nbt) ? $"Tags:[{tag}]" : $"{obj.Nbt},Tags:[{tag}]";
                        }
                    }

                    obj.Initialize();
                }


                if (obj.Children == null) continue;
                foreach (var child in obj.Children)
                {
                    queue.Enqueue(child);
                }
            }

            if (SaveFile)
            {

                // 3. File Save
                string jsonFile = JsonConvert.SerializeObject(new BdObjectData[] { bdobject.Data }, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                });

                byte[] gzip = FileProcessingHelper.CompressGzip(jsonFile);
                string base64Text = Convert.ToBase64String(gzip);

                // 이름에 _edited 붙이기
                string newFileName = Path.GetFileNameWithoutExtension(filePath) + "_edited" + Path.GetExtension(filePath);
                string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);

                await File.WriteAllTextAsync(newFilePath, base64Text);
            }

#if UNITY_EDITOR
            string debugJson = JsonConvert.SerializeObject(new BdObjectData[] { bdobject.Data }, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            });

            Debug.Log(debugJson);
#endif

            await UniTask.SwitchToMainThread();

            OnBDObjectEdited?.Invoke(bdobject);

            loadingPanel.SetActive(false);

        }

    }
}