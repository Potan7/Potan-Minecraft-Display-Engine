using BDObjectSystem.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace Animation
{
    public class AnimModel : MonoBehaviour
    {
        public string ID;
        public float[] Transforms;
        public GameObject Model;

        public void Init(float[] transforms, GameObject model, string id)
        {
            name = id;
            Model = Instantiate(model, transform);
            SetTransformation(transforms);
            ID = id;
        }

        public void SetTransformation(float[] transforms)
        {
            Transforms = transforms;
            var matrix = AffineTransformation.GetMatrix(transforms);
            AffineTransformation.ApplyMatrixToTransform(transform, matrix);
        }
    }
}