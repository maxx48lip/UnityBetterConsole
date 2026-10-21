using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityConsole
{
    /// <summary>
    /// A piece of a console log message: either text to draw or a color that applies to all following text segments.
    /// </summary>
    public struct ConsoleSegment
    {
        public string Text;
        public ConsoleTextColor? Color;

        public static implicit operator ConsoleSegment (string text) => new ConsoleSegment { Text = text };
        public static implicit operator ConsoleSegment (ConsoleTextColor color) => new ConsoleSegment { Color = color };
    }

    /// <summary>
    /// Writes log entries to the on-screen console. Only calls through this class are shown in the console UI,
    /// regular Debug.Log output is not captured. Each entry is also forwarded to the corresponding Debug method.
    /// </summary>
    public static class ConsoleLog
    {
        public enum Level
        {
            Info,
            Warning,
            Error
        }

        public struct Entry
        {
            public Level Level;
            public ConsoleSegment[] Segments;
            public DateTime Timestamp;

            /// <summary>
            /// The entry text without colors.
            /// </summary>
            public string Message => PlainText(Segments);
        }

        /// <summary>
        /// Maximum number of entries kept in the buffer.
        /// </summary>
        public static int MaxEntries { get; set; } = 200;

        private static readonly Queue<Entry> entries = new Queue<Entry>();
        private static readonly object gate = new object();

        public static void Info (string message) => Write(Level.Info, new[] { (ConsoleSegment)(message ?? string.Empty) }, Debug.Log);
        public static void Warning (string message) => Write(Level.Warning, new[] { (ConsoleSegment)(message ?? string.Empty) }, Debug.LogWarning);
        public static void Error (string message) => Write(Level.Error, new[] { (ConsoleSegment)(message ?? string.Empty) }, Debug.LogError);

        /// <summary>
        /// Writes a colored entry. A ConsoleTextColor segment sets the color for all following text segments;
        /// text before any color is drawn with the level default color.
        /// Example: Info("plain ", Orange, "orange tail")
        /// </summary>
        public static void Info (params ConsoleSegment[] segments) => Write(Level.Info, segments, Debug.Log);
        public static void Warning (params ConsoleSegment[] segments) => Write(Level.Warning, segments, Debug.LogWarning);
        public static void Error (params ConsoleSegment[] segments) => Write(Level.Error, segments, Debug.LogError);

        private static void Write (Level level, ConsoleSegment[] segments, Action<string> debugMethod)
        {
            if (segments == null || segments.Length == 0) segments = new[] { (ConsoleSegment)string.Empty };

            lock (gate)
            {
                entries.Enqueue(new Entry { Level = level, Segments = segments, Timestamp = DateTime.Now });
                while (entries.Count > MaxEntries) entries.Dequeue();
            }
            debugMethod(PlainText(segments));
        }

        internal static string PlainText (ConsoleSegment[] segments)
        {
            var builder = new StringBuilder();
            foreach (var segment in segments ?? Array.Empty<ConsoleSegment>())
                if (!string.IsNullOrEmpty(segment.Text)) builder.Append(segment.Text);
            return builder.ToString();
        }

        internal static int Count
        {
            get { lock (gate) return entries.Count; }
        }

        /// <summary>
        /// Returns a page of up to count entries. Offset counts back from the newest entry: 0 is the latest page.
        /// </summary>
        internal static IReadOnlyList<Entry> GetPage (int count, int offset)
        {
            lock (gate)
            {
                var all = new List<Entry>(entries);
                int end = Math.Max(0, all.Count - Math.Max(0, offset));
                int start = Math.Max(0, end - count);
                return all.GetRange(start, end - start);
            }
        }
    }
}
