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
        GameManager.GetManager<FileManager>().animObjList = this;
        Timeline = GameManager.GetManager<AnimManager>().Timeline;
        jump = importButton.sizeDelta.y * 1.5f;
    }

    public AnimObject AddAnimObject(string fileName)
    {
        //Debug.Log("EndAddObject: " + obj.name);

        var animObject = Instantiate(animObjectPrefab, transform);
        animObject.Init(fileName, this);
        animObject.rect.anchoredPosition = importButton.anchoredPosition;
        animObjects.Add(animObject);

        importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y - jump);
        return animObject;
    }

    public void RemoveAnimObject(AnimObject obj)
    {
        int idx = animObjects.IndexOf(obj);
        animObjects.RemoveAt(idx);

        GameManager.GetManager<BDObjectManager>().RemoveBDObject(obj.fileName);

        Destroy(obj.gameObject);

        for (int i = idx; i < animObjects.Count; i++)
        {
            animObjects[i].rect.anchoredPosition = new Vector2(animObjects[i].rect.anchoredPosition.x, animObjects[i].rect.anchoredPosition.y + jump);
        }
        importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y + jump);
    }
}
