using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BDObjectSystem
{
    public static class BdObjectHelper
    {
        private static readonly Regex tagsRegex = new Regex(@"Tags:\[([^\]]+)\]");
        private static readonly Regex uuidRegex = new Regex(@"UUID:\[I;(-?\d+),(-?\d+),(-?\d+),(-?\d+)\]");
        private const string FrameFormatString = @"\b{0}(\d+)\b";

        // reading Tags:[] and return string
        public static string GetTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = tagsRegex.Match(input);
            return match.Success ? match.Groups[1].Value : null;
        }

        // reading UUID:[] and return string
        public static string GetUuid(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            var match = uuidRegex.Match(input);
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
        public static Dictionary<string, BdObject> SetDisplayIDDictionary(BdObject root)
        {
            var idDataDict = new Dictionary<string, BdObject>();
            var queue = new Queue<BdObject>();
            queue.Enqueue(root);
        
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                
                if (obj.IsDisplay)
                {
                    idDataDict[obj.ID] = obj;
                }
                
                // BFS
                if (obj.Children == null) continue;
                foreach (var child in obj.Children)
                {
                    queue.Enqueue(child);
                }
            }
            return idDataDict;
        }

        public static List<BdObjectContainer> SetDisplayList(BdObjectContainer root)
        {
            var resultList = new List<BdObjectContainer>();
            var queue = new Queue<BdObjectContainer>();
            queue.Enqueue(root);
            
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
            
                if (obj.BdObject.IsDisplay)
                {
                    resultList.Add(obj);
                }
                
                // BFS
                if (obj.children == null) continue;
                foreach (var child in obj.children)
                {
                    queue.Enqueue(child);
                }
            }
            return resultList;
        }
    }
}
