using System.Collections.Generic;
using BDObjectSystem;
using BDObjectSystem.Utility;
using UnityEngine;

public class BDObjectAnimator
{
    public BdObjectContainer RootObject;

    public readonly Dictionary<string, BdObjectContainer> modelDict;
    private readonly HashSet<BdObjectContainer> visitedObjects = new();

    public BDObjectAnimator(BdObjectContainer rootObject)
    {
        RootObject = rootObject;
        modelDict = BdObjectHelper.SetDisplayIDDictionary(rootObject);

    }

    /// <summary>
    /// 부모를 따라 순회하며 변환을 적용합니다.
    /// </summary>
    /// <param name="bdObj"></param>
    public void ApplyTransformation(List<BdObject> target)
    {
        visitedObjects.Clear();
        // 자식에서 부모로 올라가면서 변환을 적용합니다.
        foreach (var obj in target)
        {
            if (!modelDict.TryGetValue(obj.ID, out var model)) continue;

            // 변환적용
            model.SetTransformation(obj.Transforms);

            // 부모 올라가면서 변환적용
            var modelParent = model.parent;
            var targetParent = obj.Parent;
            while (modelParent != null && targetParent != null)
            {
                if (visitedObjects.Contains(modelParent)) break;

                if (modelParent.bdObjectID == targetParent.ID)
                {
                    modelParent.SetTransformation(targetParent.Transforms);
                    visitedObjects.Add(modelParent);
                }

                modelParent = modelParent.parent;
                targetParent = targetParent.Parent;
            }
        }
    }
    

    /// <summary>
    /// 구조가 다른 BDObjectContainer를 적용합니다.
    /// 들어온 displayList를 순회하며 각 오브젝트에 부모 변환을 무시하고 적용합니다.
    /// </summary>
    /// <param name="changedBDObjects"></param>
    public void ApplyDiffrentStructureTransform(Dictionary<string, BdObjectContainer> changedBDObjects)
    {

    }

    // private bool IsSameStructure(BdObjectContainer bdObj, BdObjectContainer model)
    // {
    //     var a = bdObj;
    //     var b = model;

    //     while (a != null && b != null)
    //     {
    //         if (a.bdObjectID != b.bdObjectID) return false;
    //         a = a.parent;
    //         b = b.parent;
    //     }

    //     if (a == null && b == null) return true;

    //     return false;
    // }
}
