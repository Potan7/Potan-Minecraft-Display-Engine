using System.Collections;
using System.Linq;
using GameSystem;
using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.IO;
using System;
using Animation.AnimFrame;
using System.Collections.Generic;
using System.Text;
using BDObjectSystem;
using Minecraft;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using SFB;
using BDObjectSystem.Utility;
using LitMotion;
using LitMotion.Extensions;

namespace FileSystem.Export
{
    public class ExportManager : BaseManager
    {
        public GameObject exportPanel;
        CanvasGroup exportPanelCanvasGroup;

        ExportSettingUIManager exportSetting;

        public TextMeshProUGUI exportPathText;
        public string currentPath;
        public static readonly Regex FNumberRegex = new Regex(@"f(\d+)", RegexOptions.IgnoreCase);
        public static readonly Regex UuidExtractedFormatRegex = new Regex(@"^(-?\d+),(-?\d+),(-?\d+),(-?\d+)$", RegexOptions.Compiled);
        public static readonly Regex TagZeroEndRegex = new Regex(@".*\D0$", RegexOptions.Compiled);


        #region UI and Initialization

        private void Start()
        {
            exportSetting = GameManager.GetManager<ExportSettingUIManager>();

            string defaultPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "Result");

            SetPathText(defaultPath);

            exportPanelCanvasGroup = exportPanel.GetComponent<CanvasGroup>();

            exportPanelCanvasGroup.alpha = 0f;
            exportPanelCanvasGroup.transform.localScale = Vector3.zero;
            exportPanelCanvasGroup.interactable = false;
            exportPanel.SetActive(false); // 처음에는 비활성화
        }

        public void SetPathText(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            // Debug.Log($"Export path: {path}");
            currentPath = path.Replace("\\", "/");
            exportPathText.text = currentPath;
        }

        public void SetExportPanel(bool isShow)
        {
            // Debug.Log($"SetExportPanel: {isShow}, Current UI Status: {UIManager.CurrentUIStatus}");
            if (isShow)
            {
                exportPanel.SetActive(true);

                LSequence.Create()
                    .Append(LMotion.Create(0f, 1f, 0.2f).BindToAlpha(exportPanelCanvasGroup))
                    .Join(LMotion.Create(Vector3.zero, Vector3.one, 0.2f).WithOnComplete(() => exportPanelCanvasGroup.interactable = true).BindToLocalScale(exportPanelCanvasGroup.transform))
                    .Run();
                // UIManager.CurrentUIStatus |= UIManager.UIStatus.OnExportPanel;
                UIManager.SetUIStatus(UIManager.UIStatus.OnExportPanel, true);
            }
            else
            {
                exportPanelCanvasGroup.interactable = false;
                LSequence.Create()
                    .Append(LMotion.Create(1f, 0f, 0.2f).BindToAlpha(exportPanelCanvasGroup))
                    .Join(LMotion.Create(Vector3.one, Vector3.zero, 0.2f).WithOnComplete(() => exportPanel.SetActive(false)).BindToLocalScale(exportPanelCanvasGroup.transform))
                    .Run();
                // UIManager.CurrentUIStatus &= ~UIManager.UIStatus.OnExportPanel;
                UIManager.SetUIStatus(UIManager.UIStatus.OnExportPanel, false);
            }

            GameManager.SetPlayerInput(!isShow);
        }

        // 경로 변경하는 버튼 클릭 시 호출
        public void GetNewPath()
        {
            // await FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, Application.dataPath).ToUniTask();
            var paths = StandaloneFileBrowser.OpenFolderPanel("Select Export Folder", currentPath, false);

            if (paths.Length > 0)
            {
                //Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetPathText(paths[0]);
            }
        }

        public async void OnExportButton()
        {
            try
            {
                await ExportFile();
            }
            catch (Exception ex)
            {
                CustomLog.LogError($"Export failed: {ex}");
            }
            finally
            {
                SetExportPanel(false);
            }
        }
        #endregion

        #region Export File
        public async UniTask ExportFile()
        {
            // 1. Export path 설정 및 폴더 정리
            if (!Directory.Exists(currentPath))
                Directory.CreateDirectory(currentPath);
            DeleteFrameAndFnumberFiles(currentPath, exportSetting.frameFileName);

            // 2. 모든 애니메이션 트랙에서 명령어 생성
            List<SortedList<int, ExportFrame>> allTracks =
                GameManager.GetManager<AnimObjList>().GetAllFrames();

            var commandsByTick = GenerateCommandsFromTracks(allTracks);

            // 3. 생성된 명령어를 파일로 작성
            await WriteCommandsToFiles(commandsByTick);

            CustomLog.Log($"Export is Done! Export path: {currentPath}");
        }
        #endregion

        #region Export Functions

        private SortedList<int, List<string>> GenerateCommandsFromTracks(List<SortedList<int, ExportFrame>> allTracks)
        {
            var result = new SortedList<int, List<string>>();

            foreach (var track in allTracks)
            {
                if (track.Count == 0)
                    continue;

                // ── 3‑1. 마지막 프레임으로 시드 세팅 ──
                var keys = track.Keys.ToList();
                int lastTick = keys[^1];
                var lastFrame = track[lastTick];

                // 이전 상태 버퍼 초기화
                var prevTransforms = new Dictionary<string, Matrix4x4>();
                var prevObjects = new Dictionary<string, BdObject>();

                var prevEntities = new HashSet<string>();

                // 마지막 프레임을 ‘이전 상태’로 복사
                foreach (var kv in lastFrame.NodeDict)
                {
                    prevEntities.Add(kv.Key);
                    prevTransforms[kv.Key] = kv.Value.Transform;
                    prevObjects[kv.Key] = kv.Value.Object;
                }

                // ── 3‑2. 오름차순 한번만 순회 ──
                // track에 저장된 모든 키프레임(tick)을 시간 순서대로 순회합니다.
                foreach (int tick in keys)
                {
                    // 현재 tick에 해당하는 프레임 데이터를 가져옵니다.
                    var frame = track[tick];

                    // 현재 비교용 딕셔너리와 해시셋
                    var currTransforms = new Dictionary<string, Matrix4x4>();
                    var currObjects = new Dictionary<string, BdObject>();
                    var currEntities = new HashSet<string>();

                    // 현재 프레임에서 생성할 명령어들을 저장할 리스트
                    var cmds = new List<string>();

                    // Diff & Command 생성: 현재 프레임의 모든 노드(엔티티)를 순회하며 변경점을 찾고 명령어를 생성합니다.
                    foreach (var kv in frame.NodeDict)
                    {
                        // kv (KeyValuePair)에서 현재 노드의 정보를 추출합니다.
                        string id = kv.Key; // 엔티티의 고유 ID (UUID 또는 태그)
                        BdObject obj = kv.Value.Object; // 엔티티에 해당하는 BdObject 객체
                        Matrix4x4 xf = kv.Value.Transform; // 엔티티의 최종 변환 행렬 (월드 좌표계 기준)

                        // 현재 프레임 상태 컬렉션에 현재 노드의 정보를 기록합니다.
                        currEntities.Add(id);
                        currTransforms[id] = xf;
                        currObjects[id] = obj;

                        // 이전 프레임에 해당 오브젝트가 존재하는지 확인하기
                        bool existed = prevEntities.Contains(id);

                        bool tChanged = false;
                        bool nameChanged = false;
                        bool textureChanged = false;

                        BdObject prevObj = null;

                        // 만약 이전 프레임에도 존재했던 엔티티라면, 실제 변경이 있었는지 비교합니다.
                        if (existed)
                        {
                            // 비교 1: 행렬 비교
                            // 비교 2: 디스플레이 변경 (이름 변경 여부)
                            // 비교 3: 텍스쳐 비교 (머리 디스플레이 전용)

                            // 변환 행렬 비교: 이전 프레임의 행렬(pxf)과 현재 행렬(xf)을 비교하여 변경 여부를 tChanged에 저장합니다.
                            if (prevTransforms.TryGetValue(id, out var pxf))
                                tChanged = !MatrixHelper.MatricesAreEqual(xf, pxf);

                            if (prevObjects.TryGetValue(id, out prevObj))
                            {
                                // 이름 변경 여부: 이전 객체(prevObj)와 현재 객체(obj)의 이름을 비교하여 nameChanged에 저장합니다.
                                nameChanged = !string.Equals(prevObj.Name, obj.Name);

                                if (obj.IsHeadDisplay)
                                {
                                    // 텍스쳐 변경 여부: 이전 객체의 텍스쳐와 현재 객체의 텍스쳐를 비교하여 textureChanged에 저장합니다.
                                    textureChanged = !string.Equals(prevObj.GetHeadTexture(), obj.GetHeadTexture());
                                }
                            }
                        }

                        // 만약 변환이 변경되지 않았다면, 이 엔티티에 대한 명령어는 생성할 필요가 없으므로 다음 노드로 넘어갑니다.
                        if (!tChanged && !nameChanged && !textureChanged)
                            continue;

                        // NBT 파트 조합: 변경된 부분이 있다면 명령어에 포함될 NBT 데이터를 조합합니다.
                        var parts = new List<string>();
                        // 변환이 변경되었다면, 변환 행렬을 NBT 문자열 형식으로 포맷팅하여 parts 리스트에 추가합니다.
                        if (tChanged)
                        {
                            string p = FormatTransformation(xf, kv.Value.Interpolation);
                            // FormatTransformation 결과에서 중괄호를 제거하여 순수 NBT 내용만 추출
                            parts.Add(p);
                        }

                        if (nameChanged)
                        {
                            // 이름이 변경된 것은 디스플레이의 ID가 변경된 것으로
                            // NBT를 수정해야합니다. 디스플레이 종류별로 ID를 설정합니다.
                            if (obj.IsBlockDisplay)
                            {
                                // block_state: {Name: "minecraft:dirt"} 형태로 설정
                                // 만약 세부 설정이 존재하는 블록의 경우 {Name:"", Properties:{}} 형태로 설정
                                // 여기서는 name이 Name[Properties] 형태로 설정되어 있음

                                string namePart = $"Name:\"{obj.ParsedName}\"";
                                if (!string.IsNullOrEmpty(obj.ParsedState))
                                {
                                    // "facing=east,half=bottom" 같은 문자열을 파싱합니다.
                                    var properties = obj.ParsedState.Split(',')
                                        .Select(part =>
                                        {
                                            var kv = part.Split('=');
                                            // key=value 쌍이 맞는지 확인합니다.
                                            if (kv.Length == 2)
                                            {
                                                // value를 큰따옴표로 감싸 NBT 문자열 형식으로 만듭니다. (예: facing:"east")
                                                return $"{kv[0]}:\"{kv[1]}\"";
                                            }
                                            return null; // 형식이 맞지 않으면 무시합니다.
                                        })
                                        .Where(s => s != null);

                                    // 파싱된 속성들을 쉼표로 연결합니다.
                                    string propertiesString = string.Join(",", properties);
                                    // 최종적으로 block_state NBT를 완성합니다.
                                    parts.Add($"block_state:{{{namePart},Properties:{{{propertiesString}}}}}");
                                }
                                else
                                {
                                    // 상태값이 없는 경우 Name만 추가합니다.
                                    parts.Add($"block_state:{{{namePart}}}");
                                }
                            }
                            else if (obj.IsItemDisplay)
                            {
                                // item: {id: "minecraft:stone"} 형태로 설정
                                // 여기서는 이름이 id[display=none] 형태로 설정되어 있음 (display는 무시)

                                parts.Add($"item:{{id:\"{obj.ParsedName}\"}}");
                            }
                            else if (obj.IsTextDisplay)
                            {
                                // TODO: 텍스트 디스플레이의 경우 일단 구현 자체를 갈아엎어야해서 미구현으로 유지   
                            }

                        }

                        if (textureChanged)
                        {
                            // Debug.Log($"Texture changed for {id}: {prevObj?.GetHeadTexture()} -> {obj.GetHeadTexture()}");
                            // 만약 이전 프레임이 player_head가 아니었는데 이번에 player_head가 된거라면
                            if (!prevObj.IsHeadDisplay)
                            {

                                // 이미 위에서 parts에 item을 수정하는 부분이 추가되었음
                                // 따라서 위 부분의 item 파트에 텍스쳐를 추가해야함.

                                int itemPartIndex = parts.FindIndex(p => p.StartsWith("item:"));
                                string itemPart = itemPartIndex >= 0 ? parts[itemPartIndex] : null;

                                if (itemPart != null)
                                {
                                    // item 파트가 존재한다면, 텍스쳐를 추가합니다.
                                    // 예시: item:{components:{"profile":{properties:[{name:"textures",value:"{defaultTextureValue}"}]}}}
                                    itemPart = itemPart.TrimEnd('}');
                                    itemPart += $",components:{{\"profile\":{{properties:[{{name:\"textures\",value:\"{obj.GetHeadTexture()}\"}}]}}}}}}";
                                    parts[itemPartIndex] = itemPart;
                                }

                            }
                            else
                            {
                                // 텍스쳐값을 수정해야합니다. 텍스쳐값은 Component로 설정됩니다.
                                // 변경 예시
                                //{item:{components:{"profile":{properties:[{name:"textures",value:"{defaultTextureValue}"}]}}}}

                                parts.Add($"item:{{components:{{\"profile\":{{properties:[{{name:\"textures\",value:\"{obj.GetHeadTexture()}\"}}]}}}}}}");
                            }

                        }

                        // 조합할 NBT 파트가 하나라도 있다면, 최종 명령어를 생성합니다.
                        if (parts.Count > 0)
                        {
                            // parts 리스트의 모든 NBT 조각들을 쉼표(,)로 연결하고 중괄호({})로 감싸 완전한 NBT 문자열을 만듭니다.
                            string combined = "{" + string.Join(",", parts) + "}";
                            // 엔티티 ID, 객체 정보, 조합된 NBT를 사용하여 최종 마인크래프트 명령어를 생성합니다.
                            string cmd = GenerateCommand(id, obj, combined, exportSetting.useFindMode);
                            // 생성된 명령어가 유효하다면(null이나 빈 문자열이 아니라면) cmds 리스트에 추가합니다.
                            if (!string.IsNullOrEmpty(cmd))
                                cmds.Add(cmd);
                        }
                    }

                    if (cmds.Count == 0)
                        cmds.Add("# No changes in this frame");

                    // 결과 집계: 생성된 명령어 리스트(cmds)를 최종 결과(result)에 추가합니다.
                    // 현재 tick을 키로 하여 명령어 리스트를 저장합니다.
                    if (!result.ContainsKey(tick))
                        result[tick] = new List<string>();
                    result[tick].AddRange(cmds);

                    // 이전 상태 업데이트: 다음 프레임과의 비교를 위해 '이전 상태'를 현재 프레임의 상태로 업데이트합니다.
                    // 얕은 복사가 아닌 새로운 Dictionary와 HashSet을 생성하여 상태가 섞이지 않도록 합니다.
                    prevTransforms = new Dictionary<string, Matrix4x4>(currTransforms);
                    prevEntities = new HashSet<string>(currEntities);
                    prevObjects = new Dictionary<string, BdObject>(currObjects);
                }
            }
            return result;
        }

        private async UniTask WriteCommandsToFiles(SortedList<int, List<string>> commandsByTick)
        {
            var utf8NoBom = new UTF8Encoding(false);

            // 4. 개별 fN.mcfunction 파일 생성
            int idx = 1;
            foreach (var kv in commandsByTick)
            {
                string path = Path.Combine(currentPath, $"f{idx++}.mcfunction");
                await File.WriteAllLinesAsync(path, kv.Value, utf8NoBom);
            }

            // 5. frame.mcfunction (scoreboard) 생성
            var scoreLines = new List<string>();
            string ns = exportSetting.packNamespace;
            if (!string.IsNullOrWhiteSpace(ns) && !ns.Contains(":"))
                ns += ":";
            else if (!ns.EndsWith('/'))
                ns += "/";

            for (int i = 0; i < commandsByTick.Count; i++)
            {
                int scoreVal = exportSetting.startTick + commandsByTick.Keys[i];
                scoreLines.Add(
                    $"execute if score {exportSetting.fakePlayer} {exportSetting.scoreboardName} matches {scoreVal} run function {ns}f{i + 1}"
                );
            }

            var addtionalCommands = exportSetting.commandLineManager.commandLines;
            if (addtionalCommands.Count > 0)
            {
                scoreLines.Add("# Additional Commands");
                foreach (var cmd in addtionalCommands)
                {
                    if (!string.IsNullOrWhiteSpace(cmd.commandLineText))
                    {
                        // 명령어가 비어있지 않으면 추가
                        scoreLines.Add(cmd.commandLineText);
                    }
                }
            }

            string scoreFile = string.IsNullOrWhiteSpace(exportSetting.frameFileName)
                                 ? "frame.mcfunction"
                                 : $"{exportSetting.frameFileName}.mcfunction";
            await File.WriteAllLinesAsync(
                Path.Combine(currentPath, scoreFile), scoreLines, utf8NoBom);
        }

        public void DeleteFrameAndFnumberFiles(string folderPath, string frameFileName)
        {
            if (!Directory.Exists(folderPath)) return;

            string[] files = Directory.GetFiles(folderPath, "*.mcfunction");
            string frameFileNameToDelete = string.IsNullOrWhiteSpace(frameFileName) ? "frame.mcfunction" : $"{frameFileName}.mcfunction";

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                Match fNumberMatch = FNumberRegex.Match(fileNameWithoutExt);

                if (fileName.Equals(frameFileNameToDelete, StringComparison.OrdinalIgnoreCase) ||
                    (fNumberMatch.Success && fNumberMatch.Value.Length == fileNameWithoutExt.Length))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        CustomLog.LogError($"삭제 실패: {fileName} - {ex.Message}");
                    }
                }
            }
        }

        public string FormatTransformation(in Matrix4x4 matrix, int interpolation)
        {
            float[] elements = MatrixHelper.MatrixToArray(matrix);

            var stringParts = elements.Select(x =>
            {
                double rounded = Math.Round(x, 4);
                string s = $"{rounded}f";
                return s.Replace(".0f", "f");
            });

            string joined = string.Join(",", stringParts);

            if (interpolation > 0)
            {
                return $"interpolation_duration: {interpolation}, transformation:[{joined}]";
            }
            else
            {
                if (MinecraftFileManager.MinecraftVersion == "1.21.4")
                {
                    return $"start_interpolation:0,interpolation_duration:{interpolation},transformation:[{joined}]";
                }
                return $"transformation:[{joined}]";
            }
        }


        public string GenerateCommand(string entityId, BdObject entity, string combinedNbt, bool mode)
        {
            string entityType = entity.GetEntityType();
            if (string.IsNullOrEmpty(entityType))
            {
                CustomLog.LogWarning($"엔티티 타입이 없어 명령어 생성 실패: {entityId}");
                return null;
            }

            var uuidmatch = UuidExtractedFormatRegex.Match(entityId);
            if (uuidmatch.Success)
            {
                if (!long.TryParse(uuidmatch.Groups[1].Value, out long a) ||
                    !int.TryParse(uuidmatch.Groups[2].Value, out int b) ||
                    !int.TryParse(uuidmatch.Groups[3].Value, out int c) ||
                    !int.TryParse(uuidmatch.Groups[4].Value, out int d))
                {
                    throw new FormatException("UUID 부분 중 하나를 숫자로 변환할 수 없습니다.");
                }

                string hexA = ((ulong)a).ToString("x");
                string hexB = (b & 0xFFFFFFFF).ToString("x");
                string hexC = (c & 0xFFFFFFFF).ToString("x");
                string hexD = (d & 0xFFFFFFFF).ToString("x");
                string uuidHex = $"{hexA}-0-{hexB}-{hexC}-{hexD}";

                return $"data merge entity {uuidHex} {combinedNbt}";
            }
            else
            {
                var tags = entityId.Split(',');
                string tag = tags.FirstOrDefault(t => !TagZeroEndRegex.IsMatch(t));

                if (!string.IsNullOrEmpty(tag))
                {
                    if (!mode)
                    {
                        return $"execute as @e[type={entityType},tag={tag}] run data merge entity @s {combinedNbt}";
                    }
                    else
                    {
                        return $"data merge entity @e[type={entityType},tag={tag},limit=1] {combinedNbt}";
                    }
                }
                else
                {
                    CustomLog.LogWarning($"UUID 또는 태그를 식별할 수 없어 명령어 생성 방식 불명확: {entityId}");
                    return $"data merge entity @e[type={entityType},limit=1,sort=nearest] {combinedNbt}";
                }
            }
        }
        #endregion
    }

}