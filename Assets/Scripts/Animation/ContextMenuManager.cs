using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ContextMenuManager : BaseManager
{
    public enum ContextMenuType
    {
        NewFrame = 0,
        Frame
    }

    [Header("Context Menu")]
    public ContextMenuType currentType;
    public GameObject contextMenu;
    public RectTransform contextMenuContent;

    [Header("Context Menu Buttons")]
    public List<GameObject> contextMenuBtns;

    public TMP_InputField[] frameInfo;

    [Header("Current Context")]
    public Frame currentFrame;
    public AnimObject currentObj;
    public int animObjectsTick;

    private void Start()
    {
        for (int i = 0; i < contextMenuBtns.Count; i++)
        {
            contextMenuBtns[i].SetActive(false);
        }
        contextMenu.SetActive(false);

    }

    // 프레임 수정 가능한 메뉴
    public void ShowContextMenu(Frame thisFrame)
    {
        currentType = ContextMenuType.Frame;
        currentFrame = thisFrame;

        frameInfo[0].text = currentFrame.Tick.ToString();
        frameInfo[1].text = currentFrame.interpolation.ToString();

        SetContextMenu();
    }

    // 새로운 프레임 추가 가능한 메뉴
    public void ShowContextMenu(AnimObject obj, int tick)
    {
        currentType = ContextMenuType.NewFrame;
        currentObj = obj;
        animObjectsTick = tick;

        SetContextMenu();
    }

    // 메뉴 열기
    private void SetContextMenu()
    {
        Vector2 mousePos = Input.mousePosition;
        contextMenu.SetActive(true);

        contextMenuContent.anchoredPosition = mousePos;
        contextMenuBtns[(int)currentType].SetActive(true);
    }

    // 메뉴 닫기
    public void CloseContectMenu()
    {
        contextMenuBtns[(int)currentType].SetActive(false);
        contextMenu.SetActive(false);
    }

    public void OnAddFrameButtonClicked()
    {
        GameManager.GetManager<FileManager>().ImportFrame(currentObj, animObjectsTick);
        CloseContectMenu();
    }

    public void OnFrameTickChanged(string value)
    {
        if (int.TryParse(value, out int tick))
        {
            frameInfo[0].text = currentFrame.SetTick(tick).ToString();

        }
        else
        {
            frameInfo[0].text = currentFrame.Tick.ToString();
        }
    }

    public void OnFrameInterChanged(string value)
    {
        if (int.TryParse(value, out int inter))
        {
            currentFrame.SetInter(inter);
        }
        else
        {
            frameInfo[1].text = currentFrame.interpolation.ToString();
        }
    }
}
