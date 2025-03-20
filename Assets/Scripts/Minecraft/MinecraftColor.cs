using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    /// <summary>
    /// Minecraft ���� Enum
    /// </summary>
    public enum MinecraftColor
    {
        White,
        Orange,
        Magenta,
        LightBlue,
        Yellow,
        Lime,
        Pink,
        Gray,
        LightGray,
        Cyan,
        Purple,
        Blue,
        Brown,
        Green,
        Red,
        Black
    }

    /// <summary>
    /// Static Ŭ����: ��� Color ���� ������ �� �ֵ��� ����
    /// </summary>
    public static class MinecraftColors
    {
        public static readonly Color White = new Color32(249, 255, 254, 255);   // #F9FFFE
        public static readonly Color Orange = new Color32(249, 128, 29, 255);   // #F9801D
        public static readonly Color Magenta = new Color32(199, 74, 189, 255);  // #C74EBD
        public static readonly Color LightBlue = new Color32(58, 183, 212, 255);// #3ABBD4
        public static readonly Color Yellow = new Color32(254, 219, 61, 255);   // #FEDB3D
        public static readonly Color Lime = new Color32(128, 199, 71, 255);     // #80C71F
        public static readonly Color Pink = new Color32(243, 139, 170, 255);    // #F38BAA
        public static readonly Color Gray = new Color32(71, 71, 75, 255);       // #474F52
        public static readonly Color LightGray = new Color32(144, 144, 157, 255); // #909D97
        public static readonly Color Cyan = new Color32(22, 156, 156, 255);     // #169C9C
        public static readonly Color Purple = new Color32(137, 58, 168, 255);   // #893A8A
        public static readonly Color Blue = new Color32(60, 68, 170, 255);      // #3C44AA
        public static readonly Color Brown = new Color32(131, 84, 50, 255);     // #835432
        public static readonly Color Green = new Color32(94, 124, 22, 255);     // #5E7C16
        public static readonly Color Red = new Color32(176, 46, 38, 255);       // #B02E26
        public static readonly Color Black = new Color32(17, 17, 21, 255);      // #1D1D21
    }




    /// <summary>
    /// Enum�� Ȯ���Ͽ� Enum���� Color ���� ������ �� �ֵ��� ��
    /// </summary>
    public static class MinecraftColorExtensions
    {
        private static readonly Dictionary<string, MinecraftColor> colorMap = new(StringComparer.OrdinalIgnoreCase)
    {
            { "white", MinecraftColor.White },
            { "orange", MinecraftColor.Orange },
            { "magenta", MinecraftColor.Magenta },
            { "light_blue", MinecraftColor.LightBlue },
            { "yellow", MinecraftColor.Yellow },
            { "lime", MinecraftColor.Lime },
            { "pink", MinecraftColor.Pink },
            { "gray", MinecraftColor.Gray },
            { "light_gray", MinecraftColor.LightGray },
            { "cyan", MinecraftColor.Cyan },
            { "purple", MinecraftColor.Purple },
            { "blue", MinecraftColor.Blue },
            { "brown", MinecraftColor.Brown },
            { "green", MinecraftColor.Green },
            { "red", MinecraftColor.Red },
            { "black", MinecraftColor.Black }
    };

        public static MinecraftColor ToColorEnum(string mcColor)
        {
            return colorMap.GetValueOrDefault(mcColor, MinecraftColor.Black); // �⺻�� (���� ����)
        }

        public static Color ToColor(this MinecraftColor mcColor)
        {
            return mcColor switch
            {
                MinecraftColor.White => MinecraftColors.White,
                MinecraftColor.Orange => MinecraftColors.Orange,
                MinecraftColor.Magenta => MinecraftColors.Magenta,
                MinecraftColor.LightBlue => MinecraftColors.LightBlue,
                MinecraftColor.Yellow => MinecraftColors.Yellow,
                MinecraftColor.Lime => MinecraftColors.Lime,
                MinecraftColor.Pink => MinecraftColors.Pink,
                MinecraftColor.Gray => MinecraftColors.Gray,
                MinecraftColor.LightGray => MinecraftColors.LightGray,
                MinecraftColor.Cyan => MinecraftColors.Cyan,
                MinecraftColor.Purple => MinecraftColors.Purple,
                MinecraftColor.Blue => MinecraftColors.Blue,
                MinecraftColor.Brown => MinecraftColors.Brown,
                MinecraftColor.Green => MinecraftColors.Green,
                MinecraftColor.Red => MinecraftColors.Red,
                MinecraftColor.Black => MinecraftColors.Black,
                _ => MinecraftColors.Black // �⺻�� (���� ����)
            };
        }
    }

}

