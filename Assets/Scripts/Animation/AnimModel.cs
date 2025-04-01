using BDObjectSystem.Utility;
using UnityEngine;

namespace Animation
{
    public class AnimModel : MonoBehaviour
    {
        public string ID;
        public Matrix4x4 Transforms;
        public GameObject Model;

        public void Init(in Matrix4x4 transforms, GameObject model, string id)
        {
            name = id;
            Model = Instantiate(model, transform);

            SetTransformation(transforms);
            ID = id;
        }

        public void SetTransformation(in Matrix4x4 transforms)
        {
            Transforms = transforms;
            // AffineTransformation.ApplyMatrixToTransform(Model.transform, transforms);
            AffineTransformation.ApplyMatrixToTransform(transform, transforms);
        }
    }
}