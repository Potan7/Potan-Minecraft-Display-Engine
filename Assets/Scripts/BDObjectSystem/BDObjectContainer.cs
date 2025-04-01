using GameSystem;
using UnityEngine;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;

namespace BDObjectSystem
{
    public class BdObjectContainer : MonoBehaviour
    {
        public string bdObjectID;
        
        public BdObject BdObject;
        public DisplayObject displayObj;

        public BdObjectContainer[] children;
        public BdObjectContainer Parent;

        public Matrix4x4 transformation;

        public Vector3 pos;
        public Vector3 scale;
        public Quaternion rotation;

        void Update()
        {
            pos = transform.position;
            scale = transform.lossyScale;
            rotation = transform.localRotation;
        }

        public void Init(BdObject bdObject, BdObjectManager manager)
        {
            // 기본 정보 설정
            BdObject = bdObject;
            gameObject.name = bdObject.Name;
            bdObjectID = bdObject.ID;

            // 그룹과 디스플레이 구분 
            if (!bdObject.IsBlockDisplay && !bdObject.IsItemDisplay && !bdObject.IsTextDisplay) return;
            
            // 디스플레이 공통부분
            var typeStart = bdObject.Name.IndexOf('[');
            if (typeStart == -1)
            {
                typeStart = bdObject.Name.Length;
            }
            var modelName = bdObject.Name[..typeStart];
            var state = bdObject.Name[typeStart..];
            state = state.Replace("[", "").Replace("]", "");

            // 블록 디스플레이
            if (bdObject.IsBlockDisplay)
            {
                var obj = Instantiate(manager.blockDisplay, transform);
                obj.LoadDisplayModel(modelName, state);
                displayObj = obj;

                // blockDisplay�� ��ġ�� ���� �ϴܿ� ����
                obj.transform.localPosition = -obj.AABBBound.min/2;
            }
            // 아이템 디스플레이
            else if (bdObject.IsItemDisplay)
            {
                var obj = Instantiate(manager.itemDisplay, transform);
                obj.LoadDisplayModel(modelName, state);
                displayObj = obj;
            }
            // 텍스트 디스플레이 
            else
            {
                var obj = Instantiate(manager.textDisplay, transform);
                obj.Init(bdObject);
                displayObj = obj;

            }
        }

        // ��ó��
        public void PostProcess(BdObjectContainer[] childArray)
        {
            // ��ȯ ����� ����
            SetTransformation(BdObject.Transforms);
            children = childArray;

            //if (displayObj == null)
            //{
            //    transform.position = new Vector3(transform.position.x, transform.position.y, -transform.position.z);
            //}
        }

        public void SetTransformation(float[] mat)
        {
            transformation = AffineTransformation.GetMatrix(mat);
            AffineTransformation.ApplyMatrixToTransform(transform, transformation);
        }
    }
}
