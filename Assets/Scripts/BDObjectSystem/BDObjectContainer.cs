using GameSystem;
using UnityEngine;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using System;
using System.Collections.Generic;

namespace BDObjectSystem
{
    public class BdObjectContainer : MonoBehaviour
    {
        public string BdObjectID => BdObject.ID;
#if UNITY_EDITOR
        public string bdObjectID;
        private void OnValidate() {
            if (BdObject != null && bdObjectID != BdObject.ID)
            {
                bdObjectID = BdObject.ID;
            }
        }
#endif

        public BdObject BdObject;
        public DisplayObject displayObj;

        public BdObjectContainer[] children;
        public BdObjectContainer parent;

        public Matrix4x4 transformation;
        public Matrix4x4 parentMatrix;

        public bool IsParentNull = false;

        public enum DisplayType
        {
            None,
            Block,
            Item,
            Text
        }

        public void Init(BdObject bdObject, BdObjectManager manager)
        {
            // 기본 정보 설정
            BdObject = bdObject;
#if UNITY_EDITOR
            gameObject.name = bdObject.Name;
#endif
            // bdObjectID = bdObject.ID;

            CreateDisplayObject(bdObject, manager);
        }

        // 마지막에 호출되는 PostProcess
        public void PostProcess(BdObjectContainer[] childArray)
        {
            // 좌표 설정
            SetTransformation(BdObject.Transforms);
            children = childArray;

            //if (displayObj == null)
            //{
            //    transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
            //}
        }

        public void SetTransformation(float[] transform) => SetTransformation(MatrixHelper.GetMatrix(transform));

        public void SetTransformation(in Matrix4x4 mat)
        {
            transformation = mat;
            MatrixHelper.ApplyMatrixToTransform(transform, transformation);
        }

        public void ChangeBDObject(BdObject bdObject)
        {
            // 1. 새로운 BdObject 정보로 교체합니다.
            BdObject = bdObject;

#if UNITY_EDITOR
            gameObject.name = bdObject.Name;
#endif

            // 2. 기존에 있던 디스플레이 모델(블록, 아이템 등)을 파괴합니다.
            if (displayObj != null)
            {
                Destroy(displayObj.gameObject);
                displayObj = null;
            }

            // 3. Init 메서드의 로직을 재활용하여 새로운 디스플레이 모델을 생성합니다.
            var manager = GameManager.GetManager<BdObjectManager>();
            CreateDisplayObject(bdObject, manager);
        }

        private void CreateDisplayObject(BdObject bdObject, BdObjectManager manager)
        {
            DisplayType type = DisplayType.None;
            if (bdObject.IsBlockDisplay) type = DisplayType.Block;
            else if (bdObject.IsItemDisplay) type = DisplayType.Item;
            else if (bdObject.IsTextDisplay) type = DisplayType.Text;

            switch (type)
            {
                case DisplayType.Block:
                    var blockObj = Instantiate(manager.blockDisplay, transform);
                    blockObj.LoadDisplayModel(bdObject.ParsedName, bdObject.ParsedState);
                    displayObj = blockObj;
                    // blockDisplay의 위치를 바닥 하단에 맞춤
                    blockObj.transform.localPosition = -blockObj.AABBBound.min / 2;
                    break;
                case DisplayType.Item:
                    var itemObj = Instantiate(manager.itemDisplay, transform);
                    itemObj.LoadDisplayModel(bdObject.ParsedName, bdObject.ParsedState);
                    displayObj = itemObj;
                    break;
                case DisplayType.Text:
                    var textObj = Instantiate(manager.textDisplay, transform);
                    textObj.Init(bdObject);
                    displayObj = textObj;
                    break;
                case DisplayType.None:
                default:
                    displayObj = null;
                    break;
            }
        }

        public void ResetContainer()
        {
            if (displayObj != null)
            {
                Destroy(displayObj.gameObject);
                displayObj = null;
            }
        }
    }
}
