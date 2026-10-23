using System;
using System.Collections.Generic;

namespace UnityConsole
{
    /// <summary>
    /// Custom parameter hints for commands in the help listing and autocomplete. When no hint is set,
    /// parameters are shown from the method signature (name + type). Use it when the actual signature
    /// differs from what is convenient to show — e.g. a variable number of arguments packed into one
    /// string parameter.
    /// </summary>
    public static class CommandHints
    {
        private static readonly Dictionary<string, List<(string Name, string Type)>> Hints = new Dictionary<string, List<(string Name, string Type)>>(StringComparer.OrdinalIgnoreCase);

        // Sets the parameter list shown for commandAlias in help and autocomplete (replaces reflection).
        public static void SetParameters (string commandAlias, params (string name, string type)[] parameters) =>
            Hints[commandAlias] = new List<(string Name, string Type)>(parameters);

        internal static bool TryGetParameters (string commandAlias, out List<(string Name, string Type)> parameters) =>
            Hints.TryGetValue(commandAlias, out parameters);
    }
}
