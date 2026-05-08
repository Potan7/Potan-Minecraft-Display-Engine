using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BDObjectSystem;
using BDObjectSystem.Utility;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace FileSystem
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
        public static async UniTask<BdObject> ProcessFileAsync(string filePath, bool logJSON = false)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                byte[] rawData = File.ReadAllBytes(filePath);
                if (rawData.Length < 2) return null;

                byte[] gzipData;

                if (rawData[0] == 0x1F && rawData[1] == 0x8B)
                {
                    // 신버전: 직접 GZip
                    gzipData = rawData;
                }
                else
                {
                    // 구버전: Base64 → GZip
                    try
                    {
                        string base64Text = Encoding.UTF8.GetString(rawData).Trim();
                        gzipData = Convert.FromBase64String(base64Text);
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"[FileProcessingHelper] Base64 디코딩 실패: {filePath}");
                        return null;
                    }
                }

                // 2) GZip 해제
                byte[] decompressed;
                try
                {
                    decompressed = DecompressGzipToBytes(gzipData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FileProcessingHelper] GZip 해제 실패 ({filePath}): {e.Message}");
                    return null;
                }

                // 3) PRJ2 컨테이너 처리 (신버전 대응)
                string jsonData;
                if (decompressed.Length >= 13 && 
                    decompressed[0] == 'P' && decompressed[1] == 'R' && decompressed[2] == 'J' && decompressed[3] == '2')
                {
                    // PRJ2 포맷: [Magic(4)][Ver(2)][Unused(3)][FileNameLen(2)][FileName(N)][DataLen(4)][Data(M)]
                    // 현재 관찰된 바에 따르면 scene.json 하나만 포함된 경우가 많음.
                    // 0..3: PRJ2
                    // 4..5: Version (01 01)
                    // 6..8: Unused (00 00 00)
                    // 9..10: FileName Length (Little Endian)
                    int fileNameLen = BitConverter.ToInt16(decompressed, 9);
                    
                    // 11..11+N-1: FileName
                    int dataLenPos = 11 + fileNameLen;

                    if (fileNameLen < 0 || decompressed.Length < dataLenPos + 4)
                    {
                        Debug.LogError($"[FileProcessingHelper] PRJ2 데이터가 너무 짧음 (DataLen 위치 부족): {filePath} (Buffer: {decompressed.Length}, FileNameLen: {fileNameLen})");
                        return null;
                    }

                    // dataLenPos..dataLenPos+3: Data Length (Little Endian)
                    int dataLen = BitConverter.ToInt32(decompressed, dataLenPos);
                    int dataStartPos = dataLenPos + 4;

                    if (dataLen < 0 || decompressed.Length < dataStartPos + dataLen)
                    {
                        Debug.LogError($"[FileProcessingHelper] PRJ2 데이터 길이 오류: {filePath} (DataLen: {dataLen}, DataStart: {dataStartPos}, Buffer: {decompressed.Length})");
                        return null;
                    }

                    jsonData = Encoding.UTF8.GetString(decompressed, dataStartPos, dataLen);
                }
                else
                {
                    // 일반 JSON (구버전)
                    jsonData = Encoding.UTF8.GetString(decompressed);
                }

                if (logJSON)
                {
                    CustomLog.UnityLog($"[FileProcessingHelper] JSON Data: {jsonData}", false);
                }

                // 4) JSON → BdObject 배열 → 첫 번째를 루트로
                var bdObjectData = JsonConvert.DeserializeObject<BdObjectData[]>(jsonData);
                if (bdObjectData == null || bdObjectData.Length == 0)
                {
                    Debug.LogWarning($"BDObject가 비어있음: {filePath}");
                    return null;
                }

                var bdRoot = new BdObject(bdObjectData[0]);
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

        private static byte[] DecompressGzipToBytes(byte[] gzipData)
        {
            using var compressedStream = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            gzipStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }
        public static byte[] CompressGzip(string jsonString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(jsonString);

            using MemoryStream memoryStream = new MemoryStream();
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return memoryStream.ToArray();
        }

        /// <summary>
        /// 경로 중에 폴더가 있다면 폴더 내의 지원되는 모든 파일을 리스트에 추가하기
        /// </summary>
        /// <param name="paths">파일 및 폴더 경로가 섞인 리스트</param>
        /// <returns>폴더가 파일로 모두 변환된 새로운 리스트</returns>
        public static List<string> GetAllFileFromFolder(IEnumerable<string> paths)
        {
            var resultFiles = new List<string>();
            // FileLoadManager에 정의된 지원 확장자 목록을 가져옵니다.
            var supportedExtensions = FileLoadManager.FileExtensions;

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    // 지원하는 모든 확장자에 대해 파일을 검색하고 결과에 추가합니다.
                    foreach (var ext in supportedExtensions)
                    {
                        resultFiles.AddRange(Directory.GetFiles(path, $"*.{ext}", SearchOption.TopDirectoryOnly));
                    }
                }
                else if (File.Exists(path))
                {
                    // 단일 파일인 경우, 지원하는 확장자인지 확인 후 추가합니다.
                    var fileExt = Path.GetExtension(path).TrimStart('.');
                    if (supportedExtensions.Contains(fileExt, StringComparer.OrdinalIgnoreCase))
                    {
                        resultFiles.Add(path);
                    }
                }
            }
            return resultFiles;
        }
    }
}
