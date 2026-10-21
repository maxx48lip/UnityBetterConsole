using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityConsole
{
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
            public string Message;
            public DateTime Timestamp;
        }

        /// <summary>
        /// Maximum number of entries kept in the buffer.
        /// </summary>
        public static int MaxEntries { get; set; } = 200;

        private static readonly Queue<Entry> entries = new Queue<Entry>();
        private static readonly object gate = new object();

        public static void Info (string message) => Write(Level.Info, message, Debug.Log);
        public static void Warning (string message) => Write(Level.Warning, message, Debug.LogWarning);
        public static void Error (string message) => Write(Level.Error, message, Debug.LogError);

        private static void Write (Level level, string message, Action<string> debugMethod)
        {
            lock (gate)
            {
                entries.Enqueue(new Entry { Level = level, Message = message ?? string.Empty, Timestamp = DateTime.Now });
                while (entries.Count > MaxEntries) entries.Dequeue();
            }
            debugMethod(message);
        }

        internal static IReadOnlyList<Entry> GetRecent (int count)
        {
            lock (gate)
            {
                var all = new List<Entry>(entries);
                int start = Math.Max(0, all.Count - count);
                return all.GetRange(start, all.Count - start);
            }
        }
    }
}
