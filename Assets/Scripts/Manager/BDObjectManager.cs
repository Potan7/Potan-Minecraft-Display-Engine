using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class BDObjectManager : RootManager
{
    public Material BDObjTransportMaterial;
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;
    public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    public void AddObjects(List<BDObject> bdObjects) => StartCoroutine(AddObejctCo(bdObjects, BDObjectParent));

    //public void AddObjects(List<BDObject> bdObjects) => StartCoroutine(DebugCo(bdObjects));

    IEnumerator DebugCo(List<BDObject> bdObjects)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        yield return StartCoroutine(AddObejctCo(bdObjects, BDObjectParent));

        stopwatch.Stop();
        Debug.Log($"AddObjects Time: {stopwatch.ElapsedMilliseconds}ms");

    }

    public void AddObjectUsingOld(List<BDObject> bdObjects) => AddObjectOld(bdObjects, BDObjectParent);

    void AddObjectOld(List<BDObject> bdObjects, Transform parent)
    {
        int count = bdObjects.Count;
        for (int i = 0; i < count; i++)
        {
            var bdObject = bdObjects[i];

            var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObject);
            BDObjectList.Add(newObj);
            // 자식 오브젝트를 추가
            if (bdObject.children != null)
            {
                AddObjectOld(bdObject.children, newObj.transform);
            }
            newObj.PostProcess();
        }
    }

    IEnumerator AddObejctCo(List<BDObject> bdObjects, Transform parent)
    {
        int count = bdObjects.Count;
        for (int i = 0; i < count; i++)
        {
            var bdObject = bdObjects[i];

            var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObject);
            BDObjectList.Add(newObj);

            // 자식 오브젝트를 추가
            if (bdObject.children != null)
            {
                yield return StartCoroutine(AddObejctCo(bdObject.children, newObj.transform));
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
