using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

public class BDObjectManager : RootManager
{
    public Material BDObjTransportMaterial;
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;

    public int BDObjectCount = 0;
    //public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    [Header("Prefabs")]
    public BlockDisplay blockDisplay;
    public ItemDisplay itemDisplay;

    public MeshRenderer cubePrefab;
    public ItemModelGenerator itemPrefab;
    public BlockModelGenerator blockPrefab;
    public HeadGenerator headPrefab;

    public void AddObjectByCo(BDObject[] bdObjects) => StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

    //public void AddObjects(List<BDObject> bdObjects) => StartCoroutine(DebugCo(bdObjects));

    IEnumerator DebugCo(BDObject[] bdObjects)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        yield return StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

        stopwatch.Stop();
        CustomLog.Log($"AddObjects Time: {stopwatch.ElapsedMilliseconds}ms");

    }

    // Transform을 기본값으로 설정하기
    public void AddObjects(BDObject[] bdObjects) => AddObjects(bdObjects, BDObjectParent);

    void AddObjects(BDObject[] bdObjects, Transform parent)
    {
        // 배열을 순회하며
        int count = bdObjects.Length;
        for (int i = 0; i < count; i++)
        {
            // 오브젝트 생성
            var newObj = Instantiate(BDObjectPrefab, parent);
            newObj.Init(bdObjects[i], this);
            //BDObjectList.Add(newObj);
            BDObjectCount++;

            // 자식 오브젝트를 추가
            if (bdObjects[i].children != null)
            {
                AddObjects(bdObjects[i].children, newObj.transform);
            }

            // 자식 생성 종료 후 후처리.
            newObj.PostProcess();
        }
    }

    IEnumerator AddObejctUsingCo(BDObject[] bdObjects, Transform parent)
    {
        int count = bdObjects.Length;
        for (int i = 0; i < count; i++)
        {
            var bdObject = bdObjects[i];

            var newObj = Instantiate(BDObjectPrefab, parent);
            newObj.Init(bdObject, this);
            //BDObjectList.Add(newObj);

            // 자식 오브젝트를 추가
            if (bdObject.children != null)
            {
                yield return StartCoroutine(AddObejctUsingCo(bdObject.children, newObj.transform));
            }

            newObj.PostProcess();

            yield return null;
        }
    }

    public void ClearAllObject()
    {
        Destroy(BDObjectParent.gameObject);
        BDObjectParent = new GameObject("BDObjectParent").transform;
    }
}
