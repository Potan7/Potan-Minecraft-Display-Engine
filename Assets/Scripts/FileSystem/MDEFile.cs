using System;
using System.Collections.Generic;
using UnityEngine;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Utility;

namespace FileSystem
{
    [Serializable]
    public class MDEFile
    {
        public string name = string.Empty;
        public string version;
        public List<AnimObjectFile> animObjects;
    }

    [Serializable]
    public class AnimObjectFile
    {
        public string name;
        public BdObject model;
        public FrameFile[] frameFiles;

        public AnimObjectFile(AnimObject animObject)
        {
            SetInformation(animObject);
        }

        private void SetInformation(AnimObject animObject)
        {
            name = animObject.bdFileName;
            model = animObject.rootBDObj.BdObject;
            frameFiles = new FrameFile[animObject.frames.Count];

            var frameValues = animObject.frames.Values;
            for (int i = 0; i < animObject.frames.Count; i++)
            {
                frameFiles[i] = new FrameFile(frameValues[i]);
            }
        }
    }

    [Serializable]
    public class FrameFile
    {
        public string name;
        public int tick;
        public int interpolation;

        public Dictionary<string, float[]> worldTransforms;

        public FrameFile(Frame frame)
        {
            SetInformation(frame);
        }

        public void SetInformation(Frame frame)
        {
            name = frame.fileName;
            tick = frame.tick;
            interpolation = frame.interpolation;

            worldTransforms = new Dictionary<string, float[]>();
            foreach (var display in frame.worldTransforms)
            {
                worldTransforms[display.Key] = AffineTransformation.MatrixToArray(display.Value);
            }
        }

    }
}