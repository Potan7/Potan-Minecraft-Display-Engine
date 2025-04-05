using System;
using System.Collections.Generic;
using UnityEngine;
using Animation.AnimFrame;
using BDObjectSystem;
using BDObjectSystem.Utility;

namespace FileSystem
{
    [Serializable]
    public class MCDEFile
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

        public void SetInformation(AnimObject animObject)
        {
            name = animObject.bdFileName;
            model = animObject.animator.RootObject.BdObject;

            int frameCount = animObject.frames.Count;
            if (frameFiles == null || frameFiles.Length != frameCount)
                frameFiles = new FrameFile[frameCount];

            var frameValues = animObject.frames.Values;
            int i = 0;
            foreach (var frame in frameValues)
            {
                if (frameFiles[i] == null)
                    frameFiles[i] = new FrameFile(frame);
                else
                    frameFiles[i].SetInformation(frame);

                i++;
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

            if (worldTransforms == null)
                worldTransforms = new Dictionary<string, float[]>();
            else
                worldTransforms.Clear();

            // foreach (var display in frame.worldTransforms)
            // {
            //     if (!worldTransforms.TryGetValue(display.Key, out var array) || array == null || array.Length != 16)
            //     {
            //         array = new float[16];
            //         worldTransforms[display.Key] = array;
            //     }

            //     AffineTransformation.MatrixToArray(display.Value, array);

            // }
        }


    }
}