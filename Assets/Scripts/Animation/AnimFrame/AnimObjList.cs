using System;
using System.Collections.Generic;
using GameSystem;
using UnityEngine;
using Animation.UI;
using BDObjectSystem;
using FileSystem;

namespace Animation.AnimFrame
{
    public class AnimObjList : MonoBehaviour
    {
        public RectTransform importButton;
        public float jump;

        public AnimObject animObjectPrefab;
        public Frame framePrefab;
        public List<AnimObject> animObjects = new();

        public Timeline timeline;
        public Transform frameParent;
        

        private void Start()
        {
            GameManager.GetManager<FileLoadManager>().animObjList = this;
            timeline = GameManager.GetManager<AnimManager>().timeline;
            jump = importButton.sizeDelta.y * 1.5f;
        }

        public AnimObject AddAnimObject(string fileName)
        {
            //Debug.Log("EndAddObject: " + obj.name);

            var animObject = Instantiate(animObjectPrefab, frameParent);
            animObject.Init(fileName, this);
            animObject.rect.anchoredPosition = new Vector2(animObject.rect.anchoredPosition.x, importButton.anchoredPosition.y - 60f);
            animObjects.Add(animObject);

            importButton.anchoredPosition = new Vector2(importButton.anchoredPosition.x, importButton.anchoredPosition.y - jump);

            var animMan = GameManager.GetManager<AnimManager>();
            animMan.Tick = 0;
            animMan.timeline.SetTickTexts(0);
            
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
