using UnityEngine;
using System.Collections.Generic;

namespace TransformHandles.Utils
{
    public static class TransformUtils
    {
        // 재사용 가능한 리스트 (GC 할당 방지)
        private static readonly List<Renderer> s_rendererCache = new List<Renderer>(32);

        public static bool IsDeepParentOf(this Transform self, Transform other)
        {
            if (self == null || self == other)
            {
                return false;
            }

            return other.IsChildOf(self);
        }

        public static Bounds GetBounds(this Transform transform)
        {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);

            // Non-allocating 버전 사용
            s_rendererCache.Clear();
            transform.GetComponentsInChildren(s_rendererCache);

            if (s_rendererCache.Count == 0)
            {
                return bounds;
            }

            var averageCenter = Vector3.zero;
            var averageSize = Vector3.zero;

            for (int i = 0; i < s_rendererCache.Count; i++)
            {
                var bound = s_rendererCache[i].bounds;
                averageCenter += bound.center;
                averageSize += bound.size;
            }

            bounds.center = averageCenter / s_rendererCache.Count;
            bounds.size = averageSize / s_rendererCache.Count;

            return bounds;
        }
    }
}