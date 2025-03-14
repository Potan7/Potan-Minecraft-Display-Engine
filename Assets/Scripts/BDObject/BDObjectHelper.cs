using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BDObject
{
    public static class BdObjectHelper
    {
        private static readonly Regex tagsRegex = new Regex(@"Tags:\[([^\]]+)\]");
        private static readonly Regex uuidRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");

        // Tags:[]���� �±� ���ڿ� ����
        public static string GetTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = tagsRegex.Match(input);
            return match.Success ? match.Groups[1].Value : null;
        }

        // UUID:[]���� UUID ���ڿ� ����
        public static string GetUuid(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = uuidRegex.Match(input);
            return match.Success
                ? $"{match.Groups[1].Value},{match.Groups[2].Value},{match.Groups[3].Value},{match.Groups[4].Value}"
                : null;
        }

        /// <summary>
        /// ��ġ ��Ÿ�� �̸����� �����Ӱ� ���� ����
        /// </summary>
        public static int ExtractNumber(string input, string key, int defaultValue = 0)
        {
            var match = Regex.Match(input, $@"\b{key}(\d+)\b");
            return match.Success ? int.Parse(match.Groups[1].Value) : defaultValue;
        }

        public static string ExtractFrame(string input, string key)
        {
            var match = Regex.Match(input, $@"\b{key}(\d+)\b");
            return match.Success ? match.Groups[0].Value : null;
        }

        public static Dictionary<string, T> SetDictionary<T>(T root, Func<T, BdObject> getBdObj, Func<T, IEnumerable<T>> getChildren)
        {
            var idDataDict = new Dictionary<string, T>();
            var queue = new Queue<T>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                var bdObj = getBdObj(obj);

                if (string.IsNullOrEmpty(bdObj.ID))
                {
                    bdObj.ID = bdObj.Name;
                }

                idDataDict[bdObj.ID] = obj;


                // �ڽĵ� ť�� �߰�
                foreach (var child in getChildren(obj))
                {
                    queue.Enqueue(child);
                }
            }
            return idDataDict;
        }
    }
}
