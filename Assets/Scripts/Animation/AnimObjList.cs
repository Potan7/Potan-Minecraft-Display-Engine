using System.Collections.Generic;
using UnityEngine;

public class AnimObjList : MonoBehaviour
{
    public RectTransform importButton;
    public float jump;

    public AnimObject animObjectPrefab;
    public Frame framePrefab;
    public List<AnimObject> animObjects = new List<AnimObject>();

    public Timeline Timeline;

    private void Start()
    {
        GameManager.GetManager<BDObjectManager>().EndAddObject += AnimObjList_EndAddObject;
        Timeline = GameManager.GetManager<AnimManager>().Timeline;
        jump = importButton.sizeDelta.y * 1.5f;
    }

    private void AnimObjList_EndAddObject(BDObject obj)
    {
        Debug.Log("EndAddObject: " + obj.name);

        var animObject = Instantiate(animObjectPrefab, transform);
        animObject.Init(obj.name, this);
        animObject.rect.anchoredPosition = importButton.anchoredPosition;
        animObjects.Add(animObject);

        importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y - jump);
    }
}
