using System;
using UnityEngine;

public class BDObejctContainer : MonoBehaviour
{
    public BDObject BDObject;
    public Matrix4x4 transformation;

    public BDObejctContainer Init(BDObject bdObject)
    {
        BDObject = bdObject;

        if (bdObject.isBlockDisplay)
        {
            var block = Resources.Load<GameObject>("Prefab/Block");
            var blockObj = Instantiate(block, transform);
            blockObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
        }
        else if (bdObject.isItemDisplay)
        {
        }

        transformation = AffineTransformation.GetMatrix(BDObject.transforms);
        AffineTransformation.ApplyMatrixToTransform(transform, transformation);

        var BDObjectManager = GameManager.GetManager<BDObjectManager>();
        if (bdObject.children != null)
        {            
            foreach (var child in BDObject.children)
            {
                BDObjectManager.AddObject(transform, child);
            }
        }


        return this;
    }

}
