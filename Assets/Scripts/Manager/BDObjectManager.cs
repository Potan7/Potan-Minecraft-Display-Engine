using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class BDObjectManager : RootManager
{
    public Transform BDObjectParent;
    public BDObejctContainer BDObjectPrefab;
    public MeshRenderer BlockPrefab;
    public List<BDObejctContainer> BDObjectList = new List<BDObejctContainer>();

    public bool IsLoading { get; private set; } = false;

    public async void AddObjects(List<BDObject> bdObjects)
    {
        IsLoading = true;
        await AddObejctAync(bdObjects);
        IsLoading = false;
    }

    async Task AddObejctAync(List<BDObject> bdObjects)
    {
        foreach (var obj in bdObjects)
        {
            AddObject(BDObjectParent, obj);
            await Task.Delay(100);
        }
    }

    public void AddObject(Transform parent, BDObject bdObject)
    {
        var newObj = Instantiate(BDObjectPrefab, parent).Init(bdObject);
        BDObjectList.Add(newObj);
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
