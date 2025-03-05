using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using static Unity.VisualScripting.Metadata;

public class BDObjectManager : BaseManager
{
    // 디스플레이 생성용
    public Material BDObjTransportMaterial;
    public Material BDObjHeadMaterial;

    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;

    public int BDObjectCount = 0;
    //public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();
    public Dictionary<string, BDObejctContainer> BDObjects = new Dictionary<string, BDObejctContainer>();

    [Header("Prefabs")]
    public BlockDisplay blockDisplay;
    public ItemDisplay itemDisplay;
    public TextDisplay textDisplay;

    public MeshRenderer cubePrefab;
    public ItemModelGenerator itemPrefab;
    public BlockModelGenerator blockPrefab;
    public HeadGenerator headPrefab;

    // 최상위 BDObject를 하나 받아서 전체 계층 구조를 생성하고,
    // 해당 최상위 오브젝트만 Dictionary에 등록한다.
    public async Task<BDObejctContainer> AddObject(BDObject bdObject, string fileName)
    {
        BDObjectCount = 0;
        // 최상위 BDObject를 트리 구조로 생성
        var rootObj = await CreateObjectHierarchyAsync(bdObject, BDObjectParent);

        // 최상위 오브젝트만 Dictionary에 등록
        BDObjects[fileName] = rootObj;
        return rootObj;
    }

    // bdObject 하나를 받아 자신의 GameObject를 생성하고, 그 자식들도 재귀적으로 생성한다.
    private async Task<BDObejctContainer> CreateObjectHierarchyAsync(BDObject bdObject, Transform parent, int batchSize = 10)
    {
        // BDObjectPrefab 기반으로 인스턴스 생성
        var newObj = Instantiate(BDObjectPrefab, parent);

        // 초기화
        newObj.Init(bdObject, this);
        BDObjectCount++;

        BDObejctContainer[] children = null;
        // 자식 트리 생성
        if (bdObject.children != null && bdObject.children.Length > 0)
        {
            children = new BDObejctContainer[bdObject.children.Length];
            for (int i = 0; i < bdObject.children.Length; i++)
            {
                children[i] = await CreateObjectHierarchyAsync(bdObject.children[i], newObj.transform, batchSize);

                // 일정 개수마다 한 프레임씩 대기
                if (i % batchSize == 0)
                {
                    await Task.Yield();
                }
            }
        }

        // 개별 후처리
        newObj.PostProcess(children);

        return newObj;
    }

    //void AddObjects(BDObject[] bdObjects, Transform parent)
    //{
    //    // 배열을 순회하며
    //    int count = bdObjects.Length;
    //    for (int i = 0; i < count; i++)
    //    {
    //        // 오브젝트 생성
    //        var newObj = Instantiate(BDObjectPrefab, parent);
    //        newObj.Init(bdObjects[i], this);
    //        //BDObjectList.Add(newObj);
    //        BDObjectCount++;

    //        // 자식 오브젝트를 추가
    //        if (bdObjects[i].children != null)
    //        {
    //            AddObjects(bdObjects[i].children, newObj.transform);
    //        }

    //        // 자식 생성 종료 후 후처리.
    //        newObj.PostProcess();
    //    }
    //}

    public void ClearAllObject()
    {
        Destroy(BDObjectParent.gameObject);
        BDObjectParent = new GameObject("BDObjectParent").transform;
        BDObjectParent.localScale = new Vector3(1, 1, -1);
    }

    // 오브젝트 삭제하기
    public void RemoveBDObject(string name)
    {
        if (BDObjects.TryGetValue(name, out var obj))
        {
            BDObjects.Remove(name);
            Destroy(obj.gameObject);
        }
    }
}
