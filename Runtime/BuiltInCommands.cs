using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityConsole
{
    /// <summary>
    /// Built-in console commands shipped with the package. Their aliases are reserved: project code
    /// cannot register a [ConsoleCommand] under the same name (see CommandDatabase.RegisterCommands).
    /// </summary>
    internal static class BuiltInCommands
    {
        public const string HelpAlias = "help";
        public const string ClearAlias = "clear";

        // Commands per help page; more than this many commands makes `help` paginate.
        internal const int PageSize = 20;

        // Aliases that project code cannot use with [ConsoleCommand].
        internal static readonly string[] ReservedAliases = { HelpAlias, ClearAlias };

        // alias -> handler, merged into CommandDatabase by RegisterCommands. The help handler is not
        // invoked through the generic dispatch: its page argument is optional (see ExecuteCommand).
        internal static readonly IReadOnlyDictionary<string, MethodInfo> All = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase)
        {
            [HelpAlias] = typeof(BuiltInCommands).GetMethod(nameof(Help), BindingFlags.Public | BindingFlags.Static),
            [ClearAlias] = typeof(BuiltInCommands).GetMethod(nameof(Clear), BindingFlags.Public | BindingFlags.Static)
        };

        // Registered in the database for completion and the help listing; not invoked directly.
        public static void Help (int page) => Show(page);

        // Empties the on-screen log buffer without logging anything about it.
        public static void Clear () => ConsoleLog.Clear();

        internal static void Dispatch (string[] args)
        {
            int page = 1;
            if (args.Length > 0 && !int.TryParse(args[0], out page))
                Debug.LogWarning($"UnityConsole: `help` expects a page number, got `{args[0]}` — showing page 1.");
            Show(page);
        }

        internal static void Show (int requestedPage)
        {
            var entries = CommandDatabase.Registered
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            int total = entries.Count;
            int pages = Math.Max(1, (total + PageSize - 1) / PageSize);
            int page = Mathf.Clamp(requestedPage, 1, pages);

            // Orange header: which page is shown and how many there are in total.
            ConsoleLog.Info(ConsoleTextColor.Orange, $"help {page}/{pages}");

            // One command per line: alias + usage snippet.
            foreach (var kv in entries.Skip((page - 1) * PageSize).Take(PageSize))
                ConsoleLog.Info($"{kv.Key} {UsageSnippet(kv)}".TrimEnd());

            // Orange separator closes the listing.
            ConsoleLog.Info(ConsoleTextColor.Orange, new string('=', 60));
        }

        static string UsageSnippet (KeyValuePair<string, MethodInfo> kv)
        {
            // The built-in help takes an optional page argument.
            if (string.Equals(kv.Key, HelpAlias, StringComparison.OrdinalIgnoreCase)) return "[page]";

            // Custom hint (CommandHints.SetParameters) — shown instead of the method signature.
            if (CommandHints.TryGetParameters(kv.Key, out var hints))
                return " " + string.Join(" ", hints.Select(h => $"<{h.Name}:{h.Type}>"));

            var parameters = kv.Value.GetParameters();
            if (parameters.Length == 0) return "";
            return " " + string.Join(" ", parameters.Select(p => $"<{p.Name}:{p.ParameterType.Name}>"));
        }
    }
}
