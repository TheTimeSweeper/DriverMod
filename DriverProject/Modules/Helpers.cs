using UnityEngine;

namespace RobDriver.Modules
{
    internal static class Helpers
    {
        internal const string agilePrefix = "<style=cIsUtility>Agile.</style> ";

        internal const string whiteItemHex = "00FF66";
        internal const string greenItemHex = "00FF66";
        internal const string redItemHex = "FF0033";
        internal const string yellowItemHex = "FFFF00";
        internal const string lunarItemHex = "0066FF";
        internal const string voidItemHex = "C678B4";
        internal const string colorSuffix = "</color>";

        internal static Color whiteItemColor = new(1f, 1f, 1f);
        internal static Color greenItemColor = new(0f, 1f, 102f / 255f);
        internal static Color redItemColor = new(1f, 0f, 51f / 255f);
        internal static Color yellowItemColor = new(1f, 1f, 0f);
        internal static Color lunarItemColor = new(0f, 102f / 255f, 1f);
        internal static Color voidItemColor = new(198f / 255f, 120f / 255f, 180f / 255f);
        internal static Color badColor = new(127f / 255f, 0f, 0f);

        internal static string ScepterDescription(string desc) => "\n<color=#d299ff>SCEPTER: " + desc + "</color>";
    }
}