using System;
using System.Collections.Generic;
using Manager;
using UnityEngine;
using UnityEngine.Serialization;

namespace Animation
{
    public class AnimObjList : MonoBehaviour
    {
        public RectTransform importButton;
        public float jump;

        public AnimObject animObjectPrefab;
        public Frame framePrefab;
        public List<AnimObject> animObjects = new();

        [FormerlySerializedAs("Timeline")] public Timeline timeline;

        private void Start()
        {
            GameManager.GetManager<FileManager>().animObjList = this;
            timeline = GameManager.GetManager<AnimManager>().timeline;
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
            var idx = animObjects.IndexOf(obj);
            animObjects.RemoveAt(idx);

            GameManager.GetManager<BdObjectManager>().RemoveBdObject(obj.bdFileName);

            Destroy(obj.gameObject);

            for (var i = idx; i < animObjects.Count; i++)
            {
                animObjects[i].rect.anchoredPosition = new Vector2(animObjects[i].rect.anchoredPosition.x, animObjects[i].rect.anchoredPosition.y + jump);
            }
            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y + jump);

            CustomLog.Log("Line Removed: " + obj.bdFileName);
        }
    }
}
