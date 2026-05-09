
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurportedVersionsSO", menuName = "ScriptableObjects/SurportedVersionsSO", order = 0)]
public class SurportedVersionsSO : ScriptableObject
{
    public string surportedVersionStart;
    public string surportedVersionEnd;

    public Version StartVersion;
    public Version EndVersion;
}