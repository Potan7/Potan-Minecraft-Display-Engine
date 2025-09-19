using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BDObjectSystem.Utility
{
    public static class BdObjectHelper
    {
        private const string FrameFormatString = @"\b{0}(\d+)\b";
        public static readonly Regex NBT_TagRegex = new Regex(@"Tags:\[([^\]]+)\]");
        public static readonly Regex NBT_UUIDRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");

        // reading Tags:[] and return string
        public static string GetTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = NBT_TagRegex.Match(input);
            return match.Success ? match.Groups[1].Value : null;
        }

        // reading UUID:[] and return string
        public static string GetUuid(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = NBT_UUIDRegex.Match(input);
            return match.Success
                ? $"{match.Groups[1].Value},{match.Groups[2].Value},{match.Groups[3].Value},{match.Groups[4].Value}"
                : null;
        }

        // set bdobjects parent
        public static void SetParent(BdObject parent, BdObject target)
        {
            target.Parent = parent;

            if (target.Children == null) return;
            foreach (var child in target.Children)
            {
                SetParent(target, child);
            }
        }

        // get number in input ({key}{number})
        public static int ExtractNumber(string input, string key, int defaultValue = 0)
        {
            var match = Regex.Match(input, string.Format(FrameFormatString, key));
            return match.Success ? int.Parse(match.Groups[1].Value) : defaultValue;
        }

        // get string in input ({key}{any number})
        public static string ExtractFrame(string input, string key)
        {
            var match = Regex.Match(input, string.Format(FrameFormatString, key));
            return match.Success ? match.Groups[0].Value : null;
        }

        // making ID:obj dict
        public static Dictionary<string, BdObjectContainer> SetDisplayIDDictionary(BdObjectContainer root)
        {
            var idDataDict = new Dictionary<string, BdObjectContainer>();
            var queue = new Queue<BdObjectContainer>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();

                if (obj.BdObject.IsDisplay)
                {
                    if (idDataDict.ContainsKey(obj.BdObjectID))
                    {
                        CustomLog.LogError($"{obj.BdObjectID}가 중복됨: 애니메이션 불가능!");
                        idDataDict.Clear();
                        return idDataDict;
                    }
                    idDataDict[obj.BdObjectID] = obj;
                }

                // BFS
                if (obj.children == null) continue;
                foreach (var child in obj.children)
                {
                    queue.Enqueue(child);
                }
            }
            return idDataDict;
        }

        /// <summary>
        /// SetDisplayList: BDObject의 자식중 모든 display 오브젝트를 BFS로 탐색하여 리스트에 저장
        /// 또한 입력으로 들어온 Dictionary에 모든 ID-Matrix를 저장합니다.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public static Dictionary<string, BdObject> SetDisplayDict(BdObject root, Dictionary<string, Matrix4x4> ModelMatrix)
        {
            var resultList = new Dictionary<string, BdObject>();
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                ModelMatrix[obj.ID] = obj.Transforms.GetMatrix();

                if (obj.IsDisplay)
                {
                    resultList[obj.ID] = obj;
                }

                // BFS
                if (obj.Children == null) continue;
                foreach (var child in obj.Children)
                {
                    queue.Enqueue(child);
                }
            }
            return resultList;
        }


        /// <summary>
        /// 해당 root의 자식들 중 Tag, UUID가 존재하지 않는 오브젝트가 존재한다면 false를 반환합니다.
        /// </summary>
        /// <param name="root"> 최상위 BDObject</param>
        /// <returns></returns>
        public static bool HasVaildID(BdObject root)
        {
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();

                if (obj.IsDisplay && string.IsNullOrEmpty(obj.ID))
                {
                    return false;
                }

                // BFS
                if (obj.Children == null) continue;
                foreach (var child in obj.Children)
                {
                    queue.Enqueue(child);
                }
            }
            return true;
        }


        public enum IDValidationResult
        {
            Vaild, NoID, Mismatch
        }
        /// <summary>
        /// 해당 root의 자식들 중 기존 알고리즘과 주어진 tag, uuid가 일치하지 않는 오브젝트가 존재한다면 false를 반환합니다.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="tag"></param>
        /// <param name="uuid"></param>
        /// <returns></returns>
        public static IDValidationResult HasVaildID(BdObject root, string tag, int uuid = -1)
        {
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);

            int idx = 1;

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();

                if (obj.IsDisplay)
                {
                    if (string.IsNullOrEmpty(obj.ID))
                    {
                        return IDValidationResult.NoID;
                    }

                    if (uuid == -1)
                    {
                        string expectTag = $"{tag}0,{tag}{idx}";
                        if (obj.ID != expectTag)
                        {
                            return IDValidationResult.Mismatch;
                        }
                    }
                    else
                    {
                        string expectTag = $"{uuid},{idx},0,0";
                        if (obj.ID != expectTag)
                        {
                            return IDValidationResult.Mismatch;
                        }
                    }

                    idx++;
                }

                // BFS
                if (obj.Children == null) continue;
                foreach (var child in obj.Children)
                {
                    queue.Enqueue(child);
                }
            }
            return IDValidationResult.Vaild;
        }


        /// <summary>
        /// 최상위 부모 오브젝트(root)로부터 시작하여,
        /// 트리 구조 내의 모든 '잎(leaf) 노드'들의 월드 행렬을 구해 반환한다.
        /// </summary>
        /// <param name="root">최상위 부모 BdObject</param>
        /// <returns>잎 노드 -> 해당 월드행렬 딕셔너리</returns>
        public static Dictionary<string, Matrix4x4> GetAllLeafWorldMatrices(BdObject root)
        {
            var result = new Dictionary<string, Matrix4x4>();

            //Matrix4x4 bigMatrix = ScaleMatrixUp(Matrix4x4.identity, 10f);

            // 재귀 호출 시작: 처음 parentWorld는 단위행렬(Identity)로 시작
            TraverseAndCollectLeaf(root, Matrix4x4.identity, result);
            //TraverseAndCollectLeaf(root, bigMatrix, result);

            return result;
        }

        /// <summary>
        /// 현재 노드(node)와 누적 월드행렬(parentWorld)을 받아,
        /// 자식들이 있으면 순회하고, 없으면 잎이므로 result에 저장
        /// 이때 월드행렬은 부모의 월드행렬과 현재 노드의 로컬행렬을 곱하여 계산
        /// </summary>
        private static void TraverseAndCollectLeaf(
            BdObject node,
            Matrix4x4 parentWorld,
            Dictionary<string, Matrix4x4> result)
        {
            // 1) 현재 노드의 로컬 행렬
            Matrix4x4 localMatrix = node.Transforms.GetMatrix();

            // 2) 부모 월드행렬 x 로컬행렬 => 현재 노드의 월드행렬
            Matrix4x4 worldMatrix = parentWorld * localMatrix;

            if (node.IsDisplay)
            {
                // result에 기록
                result[node.ID] = worldMatrix;
            }
            else
            {
                // 자식이 있으면 모든 자식에 대해 재귀
                foreach (var child in node.Children)
                {
                    TraverseAndCollectLeaf(child, worldMatrix, result);
                }
            }
        }

    }
}
