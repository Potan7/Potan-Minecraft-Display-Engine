using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BDObjectSystem.Utility;
using GameSystem;
using UnityEngine;

namespace FileSystem.Helpers
{
    /// <summary>
    /// 폴더 내 frame.txt를 찾아서, 
    /// 각 줄(f#, s#, i#)을 파싱해 FrameInfo(“f키” → (tick, interpolation))를 구성
    /// </summary>
    public static class FrameDataHelper
    {
        /// <summary>
        /// [Async] 폴더 목록에서 frame.txt 찾기 → 파싱
        /// </summary>
        public static async Task TryParseFrameTxtAsync(
            List<string> paths,
            Dictionary<string, (int, int)> frameInfo,
            SettingManager settingManager
        )
        {
            frameInfo.Clear();

            foreach (var p in paths)
            {
                if (Directory.Exists(p))
                {
                    var frameFile = Directory.GetFiles(p, "frame.txt").FirstOrDefault();

                    if (!string.IsNullOrEmpty(frameFile))
                    {
                        // 1. 로그는 메인 스레드에서 출력
                        CustomLog.Log("Frame.txt Detected : " + frameFile);

                        // 2. 파싱은 백그라운드로
                        await Task.Run(() =>
                        {
                            ParseFrameFile(frameFile, frameInfo, settingManager);
                        });
                    }
                }
            }
        }

        private static void ParseFrameFile(
            string frameFile,
            Dictionary<string, (int, int)> frameInfo,
            SettingManager settingManager
            )
        {
            var lines = File.ReadLines(frameFile);

            foreach (var line in lines)
            {
                var parts = line.Split(' ');

                string frameKey = null;
                int sValue = settingManager.defaultTickInterval;
                int iValue = settingManager.defaultInterpolation;

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();

                    if (trimmed.StartsWith("f"))
                    {
                        frameKey = trimmed;
                    }
                    else if (trimmed.StartsWith("s") &&
                             int.TryParse(trimmed.Substring(1), out int s))
                    {
                        sValue = s;
                    }
                    else if (trimmed.StartsWith("i") &&
                             int.TryParse(trimmed.Substring(1), out int inter))
                    {
                        iValue = inter;
                    }
                }

                if (!string.IsNullOrEmpty(frameKey))
                {
                    frameInfo[frameKey] = (sValue, iValue);
                }
            }
        }

    }
}
