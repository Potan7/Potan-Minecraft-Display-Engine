using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Animation.AnimFrame;
using GameSystem;
using Newtonsoft.Json;
using SimpleFileBrowser;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace FileSystem
{
    public class SaveManager : BaseManager
    {
        public const string MDEFileExtension = ".mcde";
        public string MDEFilePath = string.Empty;

        private FileBrowser.Filter saveFilter = new FileBrowser.Filter("Files", MDEFileExtension);

        public MCDEFile currentMDEFile;
        public bool IsNoneSaved { get; private set; } = true;

        public void MakeNewMDEFile(string name)
        {
            currentMDEFile = new MCDEFile
            {
                name = name,
                version = Application.version,
                animObjects = new List<AnimObjectFile>()
            };
        }

        public void SetMCDEFilePath(string path)
        {
            MDEFilePath = path;
            IsNoneSaved = false;

            string fileName = Path.GetFileNameWithoutExtension(MDEFilePath);
            GameManager.GetManager<MenuBarManager>().SetCurrentFileText(fileName);
            currentMDEFile.name = fileName;
        }


        public void UpdateSaveAnimObjects(List<AnimObject> animObjects)
        {
            // 현재 리스트 크기보다 animObjects가 더 많으면 새로 생성해서 채움
            int count = animObjects.Count;

            // 기존에 있는 항목을 덮어쓰기
            for (int i = 0; i < count; i++)
            {
                if (i < currentMDEFile.animObjects.Count)
                {
                    currentMDEFile.animObjects[i].SetInformation(animObjects[i]);
                }
                else
                {
                    currentMDEFile.animObjects.Add(new AnimObjectFile(animObjects[i]));
                }
            }

            // 나머지 불필요한 객체는 제거 (필요시 재사용 풀에 보관해도 됨)
            if (currentMDEFile.animObjects.Count > count)
            {
                currentMDEFile.animObjects.RemoveRange(count, currentMDEFile.animObjects.Count - count);
            }
        }

        #region Save Logic

        // public void UpdateSaveAnimObjects(List<AnimObject> animObjects)
        // {
        //     currentMDEFile.animObjects.Clear();
        //     foreach (var animObject in animObjects)
        //     {
        //         currentMDEFile.animObjects.Add(new AnimObjectFile(animObject));
        //     }
        // }

        // MDE 파일 업데이트하고 저장.
        public void SaveMCDEFile()
        {
            UpdateSaveAnimObjects(GameManager.GetManager<AnimObjList>().animObjects);
            // Serialize currentMDEFile to JSON and save to the specified path
            // string json = JsonConvert.SerializeObject(currentMDEFile, Formatting.Indented);
            // string fullPath = System.IO.Path.Combine(path, currentMDEFile.name + ".mde");
            // System.IO.File.WriteAllText(MDEFilePath, json);

            var json = JsonConvert.SerializeObject(currentMDEFile);
            //Debug.Log(json);
            var bytes = Encoding.UTF8.GetBytes(json);

            using (var fs = new FileStream(MDEFilePath, FileMode.Create))
            using (var gzip = new GZipStream(fs, CompressionLevel.Optimal))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            CustomLog.Log($"Saved MDE file to: {MDEFilePath}");
        }


        //public void SaveMDEFile() => SaveMDEFile(MDEFilePath);
        public void SaveAsNewFile() => StartCoroutine(SaveAsNewFileCotoutine());
        private IEnumerator SaveAsNewFileCotoutine()
        {
            FileBrowser.SetFilters(false, saveFilter);
            yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, null, currentMDEFile.name);

            if (FileBrowser.Success)
            {
                // Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetMCDEFilePath(FileBrowser.Result[0]);
                SaveMCDEFile();
            }
        }
        #endregion

        #region  Load Logic

        public void LoadMCDEFile() => StartCoroutine(LoadMDEFileCoroutine());
        private IEnumerator LoadMDEFileCoroutine()
        {
            FileBrowser.SetFilters(false, saveFilter);
            yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, currentMDEFile.name);

            if (FileBrowser.Success)
            {
                // Debug.Log("Selected Folder: " + FileBrowser.Result[0]);
                SetMCDEFilePath(FileBrowser.Result[0]);
                currentMDEFile = LoadMCDEFile(MDEFilePath);

                // Load MDE file and update the game state
            }
        }

                // MDE 파일 로드.
        public MCDEFile LoadMCDEFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                CustomLog.LogError($"File not found: {filePath}");
                return null;
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open))
                using (var gzip = new GZipStream(fs, CompressionMode.Decompress))
                using (var ms = new MemoryStream())
                {
                    gzip.CopyTo(ms);
                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    var loadedMDE = JsonConvert.DeserializeObject<MCDEFile>(json);

                    CustomLog.Log($"Loaded MDE file from: {filePath}");
                    return loadedMDE;
                }
            }
            catch (Exception ex)
            {
                CustomLog.LogError($"Failed to load MDE file: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
