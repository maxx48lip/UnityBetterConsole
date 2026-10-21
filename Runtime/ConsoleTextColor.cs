using UnityEngine;

namespace UnityConsole
{
    /// <summary>
    /// Named colors available for colored console log segments.
    /// </summary>
    public enum ConsoleTextColor
    {
        Orange,
        Magenta,
        LightBlue,
        Yellow,
        Lime,
        Pink,
        Toxic,
        LightGray,
        Cyan,
        Purple,
        Blue,
        Brown,
        Green,
        Red,
        Lavender,
        White
    }

    public static class ConsoleTextColors
    {
        /// <summary>
        /// Resolves a named console color to its Color value.
        /// </summary>
        public static Color Get (ConsoleTextColor color)
        {
            string hex = color switch
            {
                ConsoleTextColor.Orange => "#F9801D",
                ConsoleTextColor.Magenta => "#C74EBD",
                ConsoleTextColor.LightBlue => "#3AB3DA",
                ConsoleTextColor.Yellow => "#FED83D",
                ConsoleTextColor.Lime => "#80C71F",
                ConsoleTextColor.Pink => "#F38BAA",
                ConsoleTextColor.Toxic => "#00FF41",
                ConsoleTextColor.LightGray => "#9D9D97",
                ConsoleTextColor.Cyan => "#169C9C",
                ConsoleTextColor.Purple => "#A43CDD",
                ConsoleTextColor.Blue => "#4852CC",
                ConsoleTextColor.Brown => "#9D653C",
                ConsoleTextColor.Green => "#71951A",
                ConsoleTextColor.Red => "#D3372E",
                ConsoleTextColor.Lavender => "#B388FF",
                ConsoleTextColor.White => "#FFFFFF",
                _ => "#FFFFFF"
            };
            return ColorUtility.TryParseHtmlString(hex, out var value) ? value : Color.white;
        }
    }
}
