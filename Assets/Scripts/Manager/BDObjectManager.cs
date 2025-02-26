using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

public class BDObjectManager : BaseManager
{
    public Material BDObjTransportMaterial;
    public Material BDObjHeadMaterial;

    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;

    public int BDObjectCount = 0;
    //public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    [Header("Prefabs")]
    public BlockDisplay blockDisplay;
    public ItemDisplay itemDisplay;
    public TextDisplay textDisplay;

    public MeshRenderer cubePrefab;
    public ItemModelGenerator itemPrefab;
    public BlockModelGenerator blockPrefab;
    public HeadGenerator headPrefab;

    public event Action<BDObject> EndAddObject;

    // Transform을 기본값으로 설정하기
    public async Task AddObjects(BDObject[] bdObjects)
    {
        await AddObjectsAsync(bdObjects, BDObjectParent);

        EndAddObject?.Invoke(bdObjects[0]);
    }

    async Task AddObjectsAsync(BDObject[] bdObjects, Transform parent)
    {
        int count = bdObjects.Length;

        for (int i = 0; i < count; i++)
        {
            // 오브젝트 생성
            var newObj = Instantiate(BDObjectPrefab, parent);
            newObj.Init(bdObjects[i], this);
            BDObjectCount++;

            // 자식 오브젝트 비동기 생성
            if (bdObjects[i].children != null)
            {
                await AddObjectsAsync(bdObjects[i].children, newObj.transform);
            }

            // 후처리 실행
            newObj.PostProcess();

            // 매 10개마다 한 프레임 쉬기
            if (i % 10 == 0)
            {
                await Task.Yield(); // 한 프레임 대기 (코루틴의 yield return null과 동일)
            }
        }
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
}
