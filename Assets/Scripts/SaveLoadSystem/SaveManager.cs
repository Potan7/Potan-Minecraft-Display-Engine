using System.Collections.Generic;
using GameSystem;
using Newtonsoft.Json;
using UnityEngine;

namespace SaveLoadSystem
{
    public class SaveManager : BaseManager
    {
        [SerializeField]
        private MDEFile currentMDEFile;

        public void MakeNewMDEFile(string name)
        {
            currentMDEFile = new MDEFile
            {
                name = name,
                version = Application.version,
                animObjects = new List<AnimObjectFile>()
            };
        }

        public void SaveMDEFile(string path, string fileName)
        {
            if (currentMDEFile == null)
            {
                Debug.LogError("No MDE file to save.");
                return;
            }

            // Serialize currentMDEFile to JSON and save to the specified path
            string json = JsonConvert.SerializeObject(currentMDEFile, Formatting.Indented);
            string fullPath = System.IO.Path.Combine(path, fileName + ".mde");
            System.IO.File.WriteAllText(fullPath, json);
        }
    }
}
