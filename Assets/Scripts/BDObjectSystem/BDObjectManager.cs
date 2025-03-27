using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using Manager;
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
        public Transform animObjectParent;
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
        public AnimModel animModelPrefab;

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

            var IDWorldDict = AffineTransformation.GetAllLeafWorldMatrices(currentBdObject.BdObject);

            // 애니메이션용 오브젝트 생성 
            var animList = new List<AnimModel>();
            var animParent = new GameObject("AnimObjectParent").transform;
            animParent.SetParent(animObjectParent);
            animParent.localScale = new Vector3(1, 1, 1);

            for (int i = 0; i < displayList.Count; i++)
            {
                var animModel = Instantiate(animModelPrefab, animParent);
                animModel.Init(IDWorldDict[displayList[i].BdObject.ID], displayList[i].displayObj.gameObject, displayList[i].BdObject.ID);
                animList.Add(animModel);
            }

            // 저장하기
            BdObjects[fileName] = new BDObjectData(currentBdObject, displayList, animParent, animList);
            currentBdObject.gameObject.SetActive(false);
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
            Destroy(animObjectParent.gameObject);
            bdObjectParent = new GameObject("BDObjectParent").transform;
            animObjectParent = new GameObject("AnimObjectParent").transform;
            bdObjectParent.localScale = new Vector3(1, 1, -1);
            animObjectParent.localScale = new Vector3(1, 1, -1);
            BdObjects.Clear();

        }

        // BDObject 제거
        public void RemoveBdObject(string bdName)
        {
            if (BdObjects.Remove(bdName, out var obj))
            {
                Destroy(obj.RootObject.gameObject);
                Destroy(obj.AnimObjectParent.gameObject);
            }
        }
    }

    [System.Serializable]
    public readonly struct BDObjectData
    {
        public readonly BdObjectContainer RootObject;
        public readonly List<BdObjectContainer> DisplayList;
        public readonly Transform AnimObjectParent;
        public readonly List<AnimModel> AnimObjects;

        public BDObjectData(BdObjectContainer rootObject, List<BdObjectContainer> displayList, Transform animObjectParent, List<AnimModel> animObjects)
        {
            RootObject = rootObject;
            DisplayList = displayList;
            AnimObjectParent = animObjectParent;
            AnimObjects = animObjects;
        }
    }
}
