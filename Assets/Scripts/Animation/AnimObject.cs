using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimObject : MonoBehaviour
{
    public RectTransform rect;
    public TextMeshProUGUI title;
    public Frame firstFrame;
    public SortedList<int, Frame> frames = new SortedList<int, Frame>();
    public string fileName;

    public int MaxTick => frames.Values[frames.Count-1].Tick;

    AnimObjList manager;

    BDObjectContainer root;
    Dictionary<string, BDObjectContainer> idDict;

    public void Init(string FileName, AnimObjList list)
    {
        title.text = FileName;
        manager = list;
        fileName = FileName;

        var bdObject = GameManager.GetManager<BDObjectManager>().BDObjects[FileName];
        root = bdObject.Item1;
        idDict = bdObject.Item2;

        firstFrame.Init(fileName, 0, GameManager.Instance.Setting.DefaultInterpolation, root.BDObject, this);
        frames[0] = firstFrame;

        AnimManager.TickChanged += OnTickChanged;

    }

    #region Transform
    void OnTickChanged(int tick)
    {
        // 틱에 맞는 프레임을 찾기
        int left = GetLeftFrame(tick);
        if (left < 0) return;
        Frame leftFrame = frames.Values[left];

        if (leftFrame.interpolation == 0 || leftFrame.Tick + leftFrame.interpolation <= tick) 
        {

            // 보간 없이 적용
            SetObjectTransformation(root.BDObject.ID, leftFrame.info);
        }
        else
        {
            // 보간 On
            float t = (float)(tick - leftFrame.Tick) / leftFrame.interpolation;

            // 첫번째 프레임은 inter값이 항상 0이라 에러날 일이 없음
            Frame before = frames.Values[left - 1];
            SetObjectTransformationInter(t, before, leftFrame);
        }

    }

    // 보간 없이 그대로 적용
    public void SetObjectTransformation(string id, BDObject obj)
    {
        if (!idDict.TryGetValue(id, out BDObjectContainer target))
        {
            Debug.Log("Target not found, name : " + id);
            return;
        }

        target.SetTransformation(obj.transforms);

        if (obj.children != null)
        {
            foreach (var child in obj.children)
            {
                SetObjectTransformation(child.ID, child);
            }
        }
    }

    public void SetObjectTransformationInter(float t, Frame a, Frame b)
        => SetObjectTransformationInter(
            root.BDObject.ID, t, 
            a.info, b.info, 
            a.IDDataDict, b.IDDataDict,
            new HashSet<string>());
    // target에 a, b를 t 비율로 보간하여 적용
    void SetObjectTransformationInter(
        string targetName, float t, 
        BDObject a, BDObject b, 
        Dictionary<string, BDObject> aDict, Dictionary<string, BDObject> bDict,
        HashSet<string> visitedNodes
        )
    {
        if (visitedNodes.Contains(targetName)) return;
        visitedNodes.Add(targetName);

        if (!idDict.TryGetValue(targetName, out BDObjectContainer target))
        {
            Debug.Log("Target not found, name : " + targetName);
            return;
        }

        // 1. transforms(4x4 행렬, float[16])을 t 비율로 보간
        float[] result = new float[16];
        for (int i = 0; i < 16; i++)
        {
            result[i] = a.transforms[i] * (1f - t) + b.transforms[i] * t;
        }

        // 2. 보간된 결과를 target에 적용
        target.SetTransformation(result);

        // 자식이 없으면 종료
        if (a.children == null && b.children == null) return;

        HashSet<string> processedKeys = new HashSet<string>();

        foreach (var key in aDict.Keys)
        {
            if (bDict.TryGetValue(key, out BDObject bChild) && aDict[key] != null)
            {
                BDObject aChild = aDict[key];
                processedKeys.Add(key);

                if (idDict.TryGetValue(key, out BDObjectContainer targetChild))
                {
                    SetObjectTransformationInter(key, t, aChild, bChild, aDict, bDict, visitedNodes);
                }
            }
        }

        foreach (var key in bDict.Keys)
        {
            if (!processedKeys.Contains(key) && aDict.TryGetValue(key, out BDObject aChild))
            {
                BDObject bChild = bDict[key];

                if (idDict.TryGetValue(key, out BDObjectContainer targetChild))
                {
                    SetObjectTransformationInter(key, t, aChild, bChild, aDict, bDict, visitedNodes);
                }
            }
        }
    }


    int GetLeftFrame(int tick)
    {

        // 1. 가장 작은 프레임보다 tick이 작으면 null 반환
        if (frames.Values[0].Tick > tick)
            return -1;

        int left = 0;
        int right = frames.Count - 1;
        var keys = frames.Keys;
        int idx = -1; // 초깃값을 -1로 설정 (유효한 인덱스가 없을 경우 대비)

        // 2. 이진 탐색으로 left 프레임 찾기
        while (left <= right)
        {
            int mid = (left + right) / 2;
            if (keys[mid] <= tick) // "<" 대신 "<=" 사용하여 정확히 tick일 경우 mid를 idx로 설정
            {
                idx = mid; // 현재 mid가 left 후보
                left = mid + 1; // 더 큰 값 탐색
            }
            else
            {
                right = mid - 1; // 더 작은 값 탐색
            }
        }

        // 3. leftIdx 설정 (idx가 -1인 경우도 고려)
        if (idx >= 0)
        {
            return idx;
        }

        return -1;
    }

    #endregion

    #region EditFrame

    // 클릭했을 때 
    public void OnEventTriggerClick(BaseEventData eventData)
    {
        PointerEventData pointerData = eventData as PointerEventData;

        if (pointerData.button == PointerEventData.InputButton.Right)
        {
            //Debug.Log("Right Click");
            var line = manager.Timeline.GetTickLine(pointerData.position);
            GameManager.GetManager<ContextMenuManager>().ShowContextMenu(this, line.Tick);
        }
    }

    // tick 위치에 프레임 추가하기. 만약 tick에 이미 프레임이 있다면 tick을 그만큼 뒤로 미룸
    // 만약 입력으로 들어온 BDObject가 firstFrame과 다른 형태라면 거부
    public void AddFrame(string fileName, BDObject frameInfo, int tick, int inter = -1)
    {
        //Debug.Log("fileName : " + fileName + ", tick : " + tick + ", inter : " + inter);

        var frame = Instantiate(manager.framePrefab, transform.GetChild(0));

        while (frames.ContainsKey(tick))
        {
            tick++;
        }

        frames.Add(tick, frame);
        frame.Init(fileName, tick, inter < 0 ? GameManager.Instance.Setting.DefaultInterpolation : inter, frameInfo, this);
    }

    // 이름에서 s, i 값을 추출하여 프레임 추가하기
    public void AddFrame(BDObject frameInfo, string fileName)
    {
        int tick = MaxTick;

        if (GameManager.Instance.Setting.UseNameInfoExtract)
        {
            int sValue = BDObjectHelper.ExtractNumber(fileName, "s", 0);
            int iValue = BDObjectHelper.ExtractNumber(fileName, "i", -1);

            if (sValue > 0)
                tick += sValue;
            else
                tick += GameManager.Instance.Setting.DefaultTickInterval;

            AddFrame(fileName, frameInfo, tick, iValue);
        }
        else
        {
            AddFrame(fileName, frameInfo, tick + GameManager.Instance.Setting.DefaultTickInterval);
        }
    }

    // 프레임 삭제하기
    public void RemoveFrame(Frame frame)
    {
        frames.Remove(frame.Tick);
        Destroy(frame.gameObject);

        if (frames.Count == 0)
        {
            RemoveAnimObj();
        }
        else if (frame.Tick == 0)
        {
            frames.Values[0].SetTick(0);
        }

    }

    // 애니메이션 오브젝트 삭제하기
    public void RemoveAnimObj()
    {
        AnimManager.TickChanged -= OnTickChanged;
        manager.RemoveAnimObject(this);
    }

    // 위치를 변경 가능한지 확인하고 변경 가능하면 frames 변경하고 true 반환
    public bool ChangePos(Frame frame, int firstTick, int changedTick)
    {
        //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
        if (firstTick == changedTick) return true;
        if (frames.ContainsKey(changedTick)) return false;

        frames.Remove(firstTick);
        frames.Add(changedTick, frame);

        OnTickChanged(GameManager.GetManager<AnimManager>().Tick);
        return true;
    }

    //bool CheckBDObject(BDObject a, BDObject b, bool IsFirst = false)
    //{
    //    //Debug.Log(a?.ToString() + " vs " + b?.ToString());

    //    // 1) 둘 다 null이면 "동일"로 간주
    //    if (a == null && b == null)
    //    {
    //        //CustomLog.Log("Both objects are null → Considered equal.");
    //        return true;
    //    }

    //    // 2) 한 쪽만 null이면 다름
    //    if (a == null || b == null)
    //    {
    //        CustomLog.LogError($"One object is null -> a: {(a == null ? "null" : a.name)}, b: {(b == null ? "null" : b.name)}");
    //        return false;
    //    }

    //    // 3) name이 다르면 바로 false
    //    if (a.name != b.name && !IsFirst)
    //    {
    //        CustomLog.LogError($"Different Name -> a: {a.name}, b: {b.name}");
    //        return false;
    //    }

    //    // 4) children 비교
    //    if (a.children == null && b.children == null)
    //    {
    //        //CustomLog.Log($"Both '{a.name}' and '{b.name}' have no children → Considered equal.");
    //        return true;
    //    }

    //    if (a.children == null || b.children == null)
    //    {
    //        CustomLog.LogError($"Children mismatch -> a: {(a.children == null ? "null" : "exists")}, b: {(b.children == null ? "null" : "exists")}");
    //        return false;
    //    }

    //    // 길이가 다르면 false
    //    if (a.children.Length != b.children.Length)
    //    {
    //        CustomLog.LogError($"Children count mismatch -> a: {a.children.Length}, b: {b.children.Length}");
    //        CustomLog.LogError($"a: {string.Join(", ", a.children.Select(c => c.name))}");
    //        CustomLog.LogError($"b: {string.Join(", ", b.children.Select(c => c.name))}");
    //        return false;
    //    }

    //    // 각각의 자식을 재귀적으로 비교
    //    for (int i = 0; i < a.children.Length; i++)
    //    {
    //        if (!CheckBDObject(a.children[i], b.children[i]))
    //        {
    //            CustomLog.LogError($"Child mismatch at index {i} → a: {a.children[i]?.name}, b: {b.children[i]?.name}");
    //            return false;
    //        }
    //    }

    //    // 위의 모든 검사를 통과하면 true
    //    return true;
    //}
    #endregion
}
