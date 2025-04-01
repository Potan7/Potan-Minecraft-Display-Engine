using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using GameSystem;
using BDObjectSystem.Display;
using BDObjectSystem.Utility;
using Animation;
using Animation.AnimFrame;

namespace BDObjectSystem
{
    public class BdObjectManager : BaseManager
    {
        #region Variables
        // BDObjects Property
        [Header("BDObject Materials")]
        [FormerlySerializedAs("BDObjTransportMaterial")] public Material bdObjTransportMaterial;
        [FormerlySerializedAs("BDObjHeadMaterial")] public Material bdObjHeadMaterial;

        [Header("Variables and Transforms")]
        public Transform bdObjectParent;
        public int bdObjectCount;
        public readonly Dictionary<string, BDObjectData> BdObjects = new();
        // public readonly Dictionary<string, (BdObjectContainer, Dictionary<string, BdObjectContainer>)> BdObjects = new();
        public BdObjectContainer currentBdObject;

        [Header("Prefabs")]
        [FormerlySerializedAs("BDObjectPrefab")] public BdObjectContainer bdObjectPrefab;
        public BlockDisplay blockDisplay;
        public ItemDisplay itemDisplay;
        public TextDisplay textDisplay;

        public MeshRenderer cubePrefab;
        public ItemModelGenerator itemPrefab;
        public BlockModelGenerator blockPrefab;
        public HeadGenerator headPrefab;

        #endregion

        #region Make BDObject


        public async Task AddObject(BdObject bdObject)
        {
            bdObjectCount = 0;
            // BDObjectContainer 생성하기 
            currentBdObject = await CreateObjectHierarchyAsync(bdObject, bdObjectParent);

            // var idDict = BdObjectHelper.SetDictionary(
            //     rootObj,
            //     obj => obj.BdObject,
            //     obj => obj.children ?? Enumerable.Empty<BdObjectContainer>()
            // );
        }

        public void EndFileImport(string fileName)
        {
            // 디스플레이만 모아놓은 리스트 생성 
            var displayList = BdObjectHelper.SetDisplayList(currentBdObject);

            AffineTransformation.SetParentInverseWorldMatrix(currentBdObject);

            // 저장하기
            BdObjects[fileName] = new BDObjectData(currentBdObject, displayList);
        }

        // BDObject 따라가면서 BDObjectContainer 생성 
        private async Task<BdObjectContainer> CreateObjectHierarchyAsync(BdObject bdObject, Transform parent, BdObjectContainer parentBdobj = null, int batchSize = 10)
        {
            // BDObjectPrefab 으로 생성하기 
            var newObj = Instantiate(bdObjectPrefab, parent);
            newObj.Parent = parentBdobj;

            // Set BDObjectContainer
            newObj.Init(bdObject, this);
            bdObjectCount++;

            BdObjectContainer[] children = null;

            // when children exist
            if (bdObject.Children is { Length: > 0 })
            {
                children = new BdObjectContainer[bdObject.Children.Length];
                for (var i = 0; i < bdObject.Children.Length; i++)
                {
                    children[i] = await CreateObjectHierarchyAsync(bdObject.Children[i], newObj.transform, newObj, batchSize);

                    // delay
                    if (i % batchSize == 0)
                    {
                        await Task.Yield();
                    }
                }
            }

            // 위치 처리 
            newObj.PostProcess(children);

            return newObj;
        }
        #endregion


        // 모든 BDObject 제거        
        public void ClearAllObject()
        {
            Destroy(bdObjectParent.gameObject);
            bdObjectParent = new GameObject("BDObjectParent").transform;
            bdObjectParent.localScale = new Vector3(1, 1, -1);
            BdObjects.Clear();

        }

        // BDObject 제거
        public void RemoveBdObject(string bdName)
        {
            if (BdObjects.Remove(bdName, out var obj))
            {
                Destroy(obj.RootObject.gameObject);
            }
        }
    }

    [System.Serializable]
    public readonly struct BDObjectData
    {
        public readonly BdObjectContainer RootObject;
        public readonly List<BdObjectContainer> DisplayList;

        public BDObjectData(BdObjectContainer rootObject, List<BdObjectContainer> displayList)
        {
            RootObject = rootObject;
            DisplayList = displayList;
        }
    }
}
