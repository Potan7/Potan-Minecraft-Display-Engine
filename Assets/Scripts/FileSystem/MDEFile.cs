using System;
using System.Collections.Generic;
using System.Numerics;
using BDObjectSystem;

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

    }

    [Serializable]
    public class FrameFile
    {
        public int tick;
        public int interpolation;

        public Dictionary<string, Matrix4x4> worldTransforms;

    }
}