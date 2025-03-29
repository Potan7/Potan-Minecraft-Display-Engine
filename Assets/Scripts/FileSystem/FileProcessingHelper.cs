using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BDObjectSystem;
using BDObjectSystem.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace FileSystem.Helpers
{
    /// <summary>
    /// 파일을 읽어 base64 → gzip 해제 → JSON → BDObject 로 변환하는 등의
    /// “순수 유틸리티 로직”을 모아둔 클래스
    /// </summary>
    public static class FileProcessingHelper
    {
        /// <summary>
        /// [Async] 파일 하나를 읽어 BDObject 배열 로드 후, 첫 번째를 반환
        /// </summary>
        public static async Task<BdObject> ProcessFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                // 1) base64 → gzip 바이트
                string base64Text = SimpleFileBrowser.FileBrowserHelpers.ReadTextFromFile(filePath);
                byte[] gzipData = Convert.FromBase64String(base64Text);

                // 2) gzip 해제 → JSON 문자열
                string jsonData = DecompressGzip(gzipData);

                // 3) JSON → BdObject 배열 → 첫 번째를 루트로
                var bdObjects = JsonConvert.DeserializeObject<BdObject[]>(jsonData);
                if (bdObjects == null || bdObjects.Length == 0)
                {
                    Debug.LogWarning($"BDObject가 비어있음: {filePath}");
                    return null;
                }

                var bdRoot = bdObjects[0];
                BdObjectHelper.SetParent(null, bdRoot);

                return bdRoot;
            });
        }

        /// <summary>
        /// “f<number>” 패턴(예: f1, f2, f10...)에 따라 파일 이름 정렬.
        /// 매칭 안 되는 파일은 뒤로 붙임.
        /// </summary>
        public static List<string> SortFiles(IEnumerable<string> fileNames)
        {
            var regex = new Regex(@"f(\d+)", RegexOptions.IgnoreCase);
            var matched = new List<(string path, int number)>();
            var unmatched = new List<string>();

            foreach (var path in fileNames)
            {
                var match = regex.Match(path);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
                    matched.Add((path, num));
                else
                    unmatched.Add(path);
            }

            // 정수 기준 정렬
            matched.Sort((a, b) => a.number.CompareTo(b.number));

            // 결과 합치기
            var sorted = matched.Select(x => x.path).ToList();
            sorted.AddRange(unmatched);

            return sorted;
        }

        /// <summary>
        /// GZip 바이트 배열을 해제해 문자열(JSON)로 반환
        /// </summary>
        private static string DecompressGzip(byte[] gzipData)
        {
            using var compressedStream = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// 경로 중에 폴더가 있다면 폴더 내의 bdengine 파일을 리스트에 추가하기
        /// 해당 폴더 경로는 제거됨.
        /// </summary>
        /// <param name="paths"></param>
        public static void GetAllFileFromFolder(ref List<string> paths)
        {
            // 새로운 리스트로 결과를 구성
            var newPaths = new List<string>();

            for (int i = paths.Count - 1; i >= 0; i--)
            {
                var path = paths[i];

                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.bdengine", SearchOption.TopDirectoryOnly);

                    if (files.Length > 0)
                    {
                        // 폴더 안의 파일들을 추가
                        newPaths.AddRange(files);
                    }

                    // 폴더는 원본 리스트에서 제거
                    paths.RemoveAt(i);
                }
            }

            // 파일들을 원본 리스트에 추가
            paths.AddRange(newPaths);
        }

    }
}
