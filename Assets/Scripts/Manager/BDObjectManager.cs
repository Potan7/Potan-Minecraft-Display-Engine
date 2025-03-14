using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BDObject;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace Manager
{
    public class BdObjectManager : BaseManager
    {
        // ���÷��� ������
        [FormerlySerializedAs("BDObjTransportMaterial")] public Material bdObjTransportMaterial;
        [FormerlySerializedAs("BDObjHeadMaterial")] [UsedImplicitly]
        public Material bdObjHeadMaterial;

        [FormerlySerializedAs("BDObjectParent")] public Transform bdObjectParent;
        [FormerlySerializedAs("BDObjectPrefab")] public BdObjectContainer bdObjectPrefab;

        [FormerlySerializedAs("BDObjectCount")] public int bdObjectCount;
        public readonly Dictionary<string, (BdObjectContainer, Dictionary<string, BdObjectContainer>)> BdObjects = new();

        [Header("Prefabs")]
        public BlockDisplay blockDisplay;
        public ItemDisplay itemDisplay;
        public TextDisplay textDisplay;

        public MeshRenderer cubePrefab;
        public ItemModelGenerator itemPrefab;
        public BlockModelGenerator blockPrefab;
        public HeadGenerator headPrefab;

        
        // ReSharper disable Unity.PerformanceAnalysis
        public async Task AddObject(BdObject bdObject, string fileName)
        {
            bdObjectCount = 0;
            // �ֻ��� BDObject�� Ʈ�� ������ ����
            var rootObj = await CreateObjectHierarchyAsync(bdObject, bdObjectParent);

            // �ֻ��� ������Ʈ�� Dictionary�� ���
            var idDict = BdObjectHelper.SetDictionary(
                rootObj,
                obj => obj.BdObject,
                obj => obj.children ?? Enumerable.Empty<BdObjectContainer>()
            );
            BdObjects[fileName] = (rootObj, idDict);
        }

        // bdObject �ϳ��� �޾� �ڽ��� GameObject�� �����ϰ�, �� �ڽĵ鵵 ��������� �����Ѵ�.
        private async Task<BdObjectContainer> CreateObjectHierarchyAsync(BdObject bdObject, Transform parent, int batchSize = 10)
        {
            // BDObjectPrefab ������� �ν��Ͻ� ����
            var newObj = Instantiate(bdObjectPrefab, parent);

            // �ʱ�ȭ
            newObj.Init(bdObject, this);
            bdObjectCount++;

            BdObjectContainer[] children = null;
            // �ڽ� Ʈ�� ����
            if (bdObject.Children is { Length: > 0 })
            {
                children = new BdObjectContainer[bdObject.Children.Length];
                for (var i = 0; i < bdObject.Children.Length; i++)
                {
                    children[i] = await CreateObjectHierarchyAsync(bdObject.Children[i], newObj.transform, batchSize);

                    // ���� �������� �� �����Ӿ� ���
                    if (i % batchSize == 0)
                    {
                        await Task.Yield();
                    }
                }
            }

            // ���� ��ó��
            newObj.PostProcess(children);

            return newObj;
        }

        //void AddObjects(BDObject[] bdObjects, Transform parent)
        //{
        //    // �迭�� ��ȸ�ϸ�
        //    int count = bdObjects.Length;
        //    for (int i = 0; i < count; i++)
        //    {
        //        // ������Ʈ ����
        //        var newObj = Instantiate(BDObjectPrefab, parent);
        //        newObj.Init(bdObjects[i], this);
        //        //BDObjectList.Add(newObj);
        //        BDObjectCount++;

        //        // �ڽ� ������Ʈ�� �߰�
        //        if (bdObjects[i].children != null)
        //        {
        //            AddObjects(bdObjects[i].children, newObj.transform);
        //        }

        //        // �ڽ� ���� ���� �� ��ó��.
        //        newObj.PostProcess();
        //    }
        //}
        
        [UsedImplicitly]
        public void ClearAllObject()
        {
            Destroy(bdObjectParent.gameObject);
            bdObjectParent = new GameObject("BDObjectParent").transform;
            bdObjectParent.localScale = new Vector3(1, 1, -1);
        }

        // ������Ʈ �����ϱ�
        public void RemoveBdObject(string bdName)
        {
            if (BdObjects.Remove(bdName, out var obj))
            {
                Destroy(obj.Item1.gameObject);
            }
        }
    }
}
