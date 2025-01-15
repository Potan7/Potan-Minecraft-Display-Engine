using UnityEngine;
using System.Collections.Generic;

public class BDObjectManager : RootManager
{
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;
    public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    public void AddObjects(List<BDObject> bdObjects)
    {
        foreach (var obj in bdObjects)
        {
            var newObj = Instantiate(BDObjectPrefab, BDObjectParent).Init(obj);
            BDObjectList.Add(newObj);
        }
        
    }

    public void AddObject(Transform parent, BDObject bdObject)
    {
        var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObject);
        BDObjectList.Add(newObj);
    }

}
