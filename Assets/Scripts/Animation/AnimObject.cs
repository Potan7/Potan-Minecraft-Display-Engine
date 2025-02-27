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

    public int MaxTick => frames[frames.Count-1].Tick;

    AnimObjList manager;

    public void Init(BDObject first, AnimObjList list, string FileName)
    {
        title.text = FileName;
        manager = list;
        fileName = FileName;

        firstFrame.Init(0, first, this);
        frames[0] = firstFrame;

        AnimManager.TickChanged += OnTickChanged;
    }

    void OnTickChanged(int tick)
    {

    }

    // tick 과 가장 가까운 두 프레임 (tick보다 왼쪽(작으면서 가장 큰 값), tick보다 오른쪽(크면서 가장 작은 값)) 구해서 반환하기
    (Frame, Frame) GetNearestFrame(int tick)
    {
        int idx = frames.IndexOfKey(tick);

        Frame left = null;
        Frame right = null;

        if (idx >= 0)
        {
            left = frames[idx];
            if (idx < frames.Count - 1)
                right = frames[idx+1];
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
            CustomLog.LogError("이 BDObject는 오브젝트와 다른 형식입니다!");
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
        //Debug.Log(a.ToString() + " " + b.ToString());
        
        // 1) 둘 다 null이면 "동일"로 간주
        if (a == null && b == null)
            return true;

        // 2) 한 쪽만 null이면 다름
        if (a == null || b == null)
            return false;

        // 3) name이 다르면 바로 false
        if (a.name != b.name && !IsFirst)
            return false;

        // 4) children 비교
        //    a.children와 b.children가 모두 null이면 문제 없음
        //    하나만 null이어도 false
        if (a.children == null && b.children == null)
            return true;  // 자식이 둘 다 없으면 name만 같은 것으로도 충분

        if (a.children == null || b.children == null)
            return false;

        // 길이가 다르면 false
        if (a.children.Length != b.children.Length)
            return false;

        // 각각의 자식을 재귀적으로 비교
        for (int i = 0; i < a.children.Length; i++)
        {
            if (!CheckBDObject(a.children[i], b.children[i]))
                return false;
        }

        // 위의 모든 검사를 통과하면 true
        return true;
    }

}
