using System.Collections.Generic;
using System;
using UnityEngine;

namespace Minecraft.MColor
{
    /// <summary>
    /// Minecraft 색상 Enum
    /// </summary>
    public enum MinecraftColor
    {
        Black,
        DarkBlue,
        DarkGreen,
        DarkAqua,
        DarkRed,
        DarkPurple,
        Gold,
        Gray,
        DarkGray,
        Blue,
        Green,
        Aqua,
        Red,
        LightPurple,
        Yellow,
        White
    }

    /// <summary>
    /// Static 클래스: 즉시 Color 값을 가져올 수 있도록 정의
    /// </summary>
    public static class MinecraftColors
    {
        public static readonly Color Black = new Color32(0, 0, 0, 255);
        public static readonly Color DarkBlue = new Color32(0, 0, 170, 255);
        public static readonly Color DarkGreen = new Color32(0, 170, 0, 255);
        public static readonly Color DarkAqua = new Color32(0, 170, 170, 255);
        public static readonly Color DarkRed = new Color32(170, 0, 0, 255);
        public static readonly Color DarkPurple = new Color32(170, 0, 170, 255);
        public static readonly Color Gold = new Color32(255, 170, 0, 255);
        public static readonly Color Gray = new Color32(170, 170, 170, 255);
        public static readonly Color DarkGray = new Color32(85, 85, 85, 255);
        public static readonly Color Blue = new Color32(85, 85, 255, 255);
        public static readonly Color Green = new Color32(85, 255, 85, 255);
        public static readonly Color Aqua = new Color32(85, 255, 255, 255);
        public static readonly Color Red = new Color32(255, 85, 85, 255);
        public static readonly Color LightPurple = new Color32(255, 85, 255, 255);
        public static readonly Color Yellow = new Color32(255, 255, 85, 255);
        public static readonly Color White = new Color32(255, 255, 255, 255);
    }



    /// <summary>
    /// Enum을 확장하여 Enum에서 Color 값을 가져올 수 있도록 함
    /// </summary>
    public static class MinecraftColorExtensions
    {
        private static readonly Dictionary<string, MinecraftColor> colorMap = new Dictionary<string, MinecraftColor>(StringComparer.OrdinalIgnoreCase)
    {
        { "black", MinecraftColor.Black },
        { "dark_blue", MinecraftColor.DarkBlue },
        { "dark_green", MinecraftColor.DarkGreen },
        { "dark_aqua", MinecraftColor.DarkAqua },
        { "dark_red", MinecraftColor.DarkRed },
        { "dark_purple", MinecraftColor.DarkPurple },
        { "gold", MinecraftColor.Gold },
        { "gray", MinecraftColor.Gray },
        { "dark_gray", MinecraftColor.DarkGray },
        { "blue", MinecraftColor.Blue },
        { "green", MinecraftColor.Green },
        { "aqua", MinecraftColor.Aqua },
        { "red", MinecraftColor.Red },
        { "light_purple", MinecraftColor.LightPurple },
        { "yellow", MinecraftColor.Yellow },
        { "white", MinecraftColor.White }
    };

        public static MinecraftColor ToColorEnum(string mcColor)
        {
            if (colorMap.TryGetValue(mcColor, out MinecraftColor color))
            {
                return color;
            }
            return MinecraftColor.Black; // 기본값 (예외 방지)
        }

        public static Color ToColor(this MinecraftColor mcColor)
        {
            return mcColor switch
            {
                MinecraftColor.Black => MinecraftColors.Black,
                MinecraftColor.DarkBlue => MinecraftColors.DarkBlue,
                MinecraftColor.DarkGreen => MinecraftColors.DarkGreen,
                MinecraftColor.DarkAqua => MinecraftColors.DarkAqua,
                MinecraftColor.DarkRed => MinecraftColors.DarkRed,
                MinecraftColor.DarkPurple => MinecraftColors.DarkPurple,
                MinecraftColor.Gold => MinecraftColors.Gold,
                MinecraftColor.Gray => MinecraftColors.Gray,
                MinecraftColor.DarkGray => MinecraftColors.DarkGray,
                MinecraftColor.Blue => MinecraftColors.Blue,
                MinecraftColor.Green => MinecraftColors.Green,
                MinecraftColor.Aqua => MinecraftColors.Aqua,
                MinecraftColor.Red => MinecraftColors.Red,
                MinecraftColor.LightPurple => MinecraftColors.LightPurple,
                MinecraftColor.Yellow => MinecraftColors.Yellow,
                MinecraftColor.White => MinecraftColors.White,
                _ => MinecraftColors.Black // 기본값 (예외 방지)
            };
        }
    }

}

