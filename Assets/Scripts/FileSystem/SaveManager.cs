using System.Collections.Generic;
using GameSystem;
using Newtonsoft.Json;
using UnityEngine;

namespace FileSystem
{
    public class SaveManager : BaseManager
    {
        public MDEFile currentMDEFile;
        public bool IsNoneSaved => string.IsNullOrEmpty(currentMDEFile.name);

        public void MakeNewMDEFile(string name)
        {
            currentMDEFile = new MDEFile
            {
                name = name,
                version = Application.version,
                animObjects = new List<AnimObjectFile>()
            };
        }

        public void SaveMDEFile(string path)
        {
            // Serialize currentMDEFile to JSON and save to the specified path
            string json = JsonConvert.SerializeObject(currentMDEFile, Formatting.Indented);
            string fullPath = System.IO.Path.Combine(path, currentMDEFile.name + ".mde");
            System.IO.File.WriteAllText(fullPath, json);
        }
    }
}
