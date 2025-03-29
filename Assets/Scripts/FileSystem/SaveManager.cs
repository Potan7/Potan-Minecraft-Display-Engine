using System.Collections;
using System.Collections.Generic;
using Animation.AnimFrame;
using GameSystem;
using Newtonsoft.Json;
using SimpleFileBrowser;
using UnityEngine;

namespace FileSystem
{
    public class SaveManager : BaseManager
    {
        public const string MDEFileExtension = ".mcde";
        public string MDEFilePath = string.Empty;

        private FileBrowser.Filter saveFilter = new FileBrowser.Filter("Files", MDEFileExtension);
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public MDEFile currentMDEFile;
        public bool IsNoneSaved {get; private set; } = true;

        public void MakeNewMDEFile(string name)
        {
            currentMDEFile = new MDEFile
            {
                name = name,
                version = Application.version,
                animObjects = new List<AnimObjectFile>()
            };
        }

        public void UpdateSaveAnimObjects(List<AnimObject> animObjects)
        {
            currentMDEFile.animObjects.Clear();
            foreach (var animObject in animObjects)
            {
                currentMDEFile.animObjects.Add(new AnimObjectFile(animObject));
            }
        }

        public void SaveMDEFile()
        {
            UpdateSaveAnimObjects(GameManager.GetManager<AnimObjList>().animObjects);
            // Serialize currentMDEFile to JSON and save to the specified path
            string json = JsonConvert.SerializeObject(currentMDEFile, Formatting.Indented, settings);
            //string fullPath = System.IO.Path.Combine(path, currentMDEFile.name + ".mde");
            System.IO.File.WriteAllText(MDEFilePath, json);

            CustomLog.Log($"Saved MDE file to: {MDEFilePath}");
        }

        public void SetMDEFilePath(string path)
        {
            MDEFilePath = path;
            IsNoneSaved = false;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(MDEFilePath);
            GameManager.GetManager<MenuBarManager>().SetCurrentFileText(fileName);
            currentMDEFile.name = fileName;
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
                SetMDEFilePath(FileBrowser.Result[0]);
                SaveMDEFile();
            }
        }
    }
}
