using System.Collections.Generic;
using System.Linq;
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

    public BDObejctContainer objectContainer;

    public int MaxTick => frames.Values[frames.Count-1].Tick;

    AnimObjList manager;

    public void Init(BDObejctContainer bdObejct, AnimObjList list, string FileName)
    {
        title.text = FileName;
        manager = list;
        fileName = FileName;

        objectContainer = bdObejct;

        firstFrame.Init(0, bdObejct.BDObject, this);
        frames[0] = firstFrame;

        AnimManager.TickChanged += OnTickChanged;
    }

    void OnTickChanged(int tick)
    {
        // 틱에 맞는 프레임을 찾기
        var (left, right) = GetNearestFrame(tick);

        if (right == null)
        {
            // 타임라인이 가장 오른쪽 프레임을 넘어섰거나 정확히 일치하는 프레임이 있을 때
            SetObjectTransformation(objectContainer, left.info);
        }
        else if (left == null)
        {
            // 타임 바가 가장 왼쪽 프레임을 넘어섬
            SetObjectTransformation(objectContainer, right.info);
        }
        else
        {
            // 두 프레임 사이에 있음
            float t = (float)(tick - left.Tick) / (right.Tick - left.Tick);

        }

    }

    // 보간 없이 그대로 적용
    public void SetObjectTransformation(BDObejctContainer target, BDObject obj)
    {
        target.SetTransformation(obj.transforms);

        if (obj.children != null)
        {
            for (int i = 0; i < obj.children.Length; i++)
            {
                SetObjectTransformation(target.children[i], obj.children[i]);
            }
        }
    }

    // target에 a, b를 t 비율로 보간하여 적용
    public void SetObjectTransformationInter(BDObejctContainer target, float t, BDObject a, BDObject b)
    {
        // 1. transforms(4x4 행렬, float[16])을 t 비율로 보간
        float[] result = new float[16];
        for (int i = 0; i < 16; i++)
        {
            // 선형 보간: (1 - t) * a + t * b
            result[i] = a.transforms[i] * (1f - t) + b.transforms[i] * t;
        }

        // 2. 보간된 결과를 target에 적용
        target.SetTransformation(result);

        // 3. 자식이 있다면, 동일하게 재귀적으로 처리
        //    a.children와 b.children의 구조가 같다고 가정
        if (a.children != null && b.children != null)
        {
            for (int i = 0; i < a.children.Length; i++)
            {
                // 자식도 같은 방식으로 보간
                SetObjectTransformationInter(target.children[i], t, a.children[i], b.children[i]);
            }
        }
    }

    // tick 과 가장 가까운 두 프레임 (tick보다 왼쪽(작으면서 가장 큰 값), tick보다 오른쪽(크면서 가장 작은 값)) 구해서 반환하기
    (Frame, Frame) GetNearestFrame(int tick)
    {
        if (frames.Values[0].Tick < tick)
            return (null, frames.Values[0]);
        else if (MaxTick > tick)
            return (frames.Values[frames.Count - 1], null);

        int idx = frames.IndexOfKey(tick);

        Frame left = null;
        Frame right = null;

        if (idx >= 0)
        {
            left = frames.Values[idx];
            right = null;
            //if (idx < frames.Count - 1)
            //    right = frames[idx+1];
        }
        else
        {
            // tick 키가 없다면, ~idx가 "삽입 위치"가 됨
            int insertionIndex = ~idx;

            // 왼쪽 인덱스는 insertionIndex - 1 (그럼 frames.Keys[leftIndex] < tick)
            int leftIndex = insertionIndex - 1;
            if (leftIndex >= 0)
            {
                left = frames.Values[leftIndex];
            }

            // 오른쪽 인덱스는 insertionIndex (그럼 frames.Keys[insertionIndex] > tick)
            if (insertionIndex < frames.Count)
            {
                right = frames.Values[insertionIndex];
            }
        }

        Debug.Log($"Tick: {tick}, Left: {left?.Tick}, Right: {right?.Tick}, idx: {~idx}");
        return (left, right);
    }

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
    public void AddFrame(BDObject frameInfo, int tick)
    {
        if (!CheckBDObject(firstFrame.info, frameInfo, true))
        {
            return;
        }

        var frame = Instantiate(manager.framePrefab, transform.GetChild(0));

        while (frames.ContainsKey(tick))
        {
            tick++;
        }

        frames.Add(tick, frame);
        frame.Init(tick, frameInfo, this);
    }

    public void AddFrame(BDObject frameInfo) => AddFrame(frameInfo, MaxTick + GameManager.Instance.Setting.DefaultTickInterval);

    // 위치를 변경 가능한지 확인하고 변경 가능하면 frames 변경하고 true 반환
    public bool ChangePos(Frame frame, int firstTick, int changedTick)
    {
        //Debug.Log("firstTick : " + firstTick + ", changedTick : " +  changedTick);
        if (firstTick == changedTick) return true;
        if (frames.ContainsKey(changedTick)) return false;

        frames.Remove(firstTick);
        frames.Add(changedTick, frame);
        return true;
    }

    // 두 BDObject를 비교하여 name이 다르면 false 반환, children까지 확인한다.
    // 첫번째일 경우 이름 비교 패스
    bool CheckBDObject(BDObject a, BDObject b, bool IsFirst = false)
    {
        //Debug.Log(a?.ToString() + " vs " + b?.ToString());

        // 1) 둘 다 null이면 "동일"로 간주
        if (a == null && b == null)
        {
            //CustomLog.Log("Both objects are null → Considered equal.");
            return true;
        }

        // 2) 한 쪽만 null이면 다름
        if (a == null || b == null)
        {
            CustomLog.LogError($"One object is null -> a: {(a == null ? "null" : a.name)}, b: {(b == null ? "null" : b.name)}");
            return false;
        }

        // 3) name이 다르면 바로 false
        if (a.name != b.name && !IsFirst)
        {
            CustomLog.LogError($"Different Name -> a: {a.name}, b: {b.name}");
            return false;
        }

        // 4) children 비교
        if (a.children == null && b.children == null)
        {
            //CustomLog.Log($"Both '{a.name}' and '{b.name}' have no children → Considered equal.");
            return true;
        }

        if (a.children == null || b.children == null)
        {
            CustomLog.LogError($"Children mismatch -> a: {(a.children == null ? "null" : "exists")}, b: {(b.children == null ? "null" : "exists")}");
            return false;
        }

        // 길이가 다르면 false
        if (a.children.Length != b.children.Length)
        {
            CustomLog.LogError($"Children count mismatch -> a: {a.children.Length}, b: {b.children.Length}");
            CustomLog.LogError($"a: {string.Join(", ", a.children.Select(c => c.name))}");
            CustomLog.LogError($"b: {string.Join(", ", b.children.Select(c => c.name))}");
            return false;
        }

        // 각각의 자식을 재귀적으로 비교
        for (int i = 0; i < a.children.Length; i++)
        {
            if (!CheckBDObject(a.children[i], b.children[i]))
            {
                CustomLog.LogError($"Child mismatch at index {i} → a: {a.children[i]?.name}, b: {b.children[i]?.name}");
                return false;
            }
        }

        // 위의 모든 검사를 통과하면 true
        return true;
    }


}
