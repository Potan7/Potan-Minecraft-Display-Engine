using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using SFB;
using TMPro;
using UnityEngine;

namespace Mainmenu
{
    public class VersionLoadPanel : MonoBehaviour
    {
        MainmenuManager manager;
        public GameObject versionLoadPanel;

        public TMP_InputField inputField;

        const string helpURL = "https://potangaming.tistory.com/319";

        // readonly FileBrowser.Filter loadFilter = new FileBrowser.Filter("Files", ".jar");
        // readonly ExtensionFilter extension = new ExtensionFilter("Jar Files", ".jar");

        void Start()
        {
            manager = GetComponent<MainmenuManager>();

            versionLoadPanel.SetActive(false);
        }

        public void OnPanelButton()
        {
            versionLoadPanel.SetActive(true);
            inputField.text = PlayerPrefs.GetString("MinecraftPath", string.Empty);

        }

        public void OnLoadByFileBrowserButton()
        {
            // FileBrowser.SetFilters(false, loadFilter);
            // await FileBrowser.WaitForLoadDialog(
            //     FileBrowser.PickMode.Files,
            //     false,
            //     string.IsNullOrEmpty(inputField.text) ? null : inputField.text,
            //     "Select Minecraft Jar File",
            //     "Select").ToUniTask();

            var paths = StandaloneFileBrowser.OpenFilePanel("Select Minecraft Jar File",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "jar", false);

            if (paths.Length > 0)
            {
                SetPath(paths[0]);
            }
        }

        public void SetPath(string path)
        {
            inputField.text = path;
        }

        public void OnHelpButton()
        {
            Application.OpenURL(helpURL);
        }

        public async void OnChangePathButton()
        {
            bool success = await manager.SetNewPath(inputField.text);
            if (success)
            {
                versionLoadPanel.SetActive(false);
            }
            else
            {
                inputField.text = string.Empty;
            }
        }

        public void OnPanelCloseButton()
        {
            versionLoadPanel.SetActive(false);
        }
    }
}

