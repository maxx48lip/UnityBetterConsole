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
        Gray,
        LightGray,
        Cyan,
        Purple,
        Blue,
        Brown,
        Green,
        Red,
        Black,
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
                ConsoleTextColor.Gray => "#474F52",
                ConsoleTextColor.LightGray => "#9D9D97",
                ConsoleTextColor.Cyan => "#169C9C",
                ConsoleTextColor.Purple => "#8932B8",
                ConsoleTextColor.Blue => "#3C44AA",
                ConsoleTextColor.Brown => "#835432",
                ConsoleTextColor.Green => "#5E7C16",
                ConsoleTextColor.Red => "#B02E26",
                ConsoleTextColor.Black => "#1D1D21",
                ConsoleTextColor.White => "#FFFFFF",
                _ => "#FFFFFF"
            };
            return ColorUtility.TryParseHtmlString(hex, out var value) ? value : Color.white;
        }
    }
}
