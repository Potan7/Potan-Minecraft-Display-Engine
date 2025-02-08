using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BDObjectManager : RootManager
{
    public Material BDObjTransportMaterial;
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;
    public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    public void AddObjectByCo(BDObject[] bdObjects) => StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

    //public void AddObjects(List<BDObject> bdObjects) => StartCoroutine(DebugCo(bdObjects));

    IEnumerator DebugCo(BDObject[] bdObjects)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        yield return StartCoroutine(AddObejctUsingCo(bdObjects, BDObjectParent));

        stopwatch.Stop();
        Debug.Log($"AddObjects Time: {stopwatch.ElapsedMilliseconds}ms");

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
            var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObjects[i]);
            BDObjectList.Add(newObj);

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

            var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObject);
            BDObjectList.Add(newObj);

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
        foreach (var obj in BDObjectList)
        {
            Destroy(obj.gameObject);
        }
        BDObjectList.Clear();
    }
}
