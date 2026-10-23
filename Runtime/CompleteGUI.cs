using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityConsole
{
    internal static class CompleteGUI
    {
        private struct Option
        {
            public string MethodName, Label;
            public string[] Parameters;
            public int ArgCount;
        }

        private static readonly List<Option> options = new List<Option>();
        private static string cachedInput;
        private static float maxWidth;
        private static bool blinkOn;

        public static void Draw (ref string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            var newBlink = Mathf.PingPong(Time.time * 2f, 1f) > .5f; // toggles every half second
            bool blinkFlipped = newBlink != blinkOn;
            blinkOn = newBlink;

            if (!string.Equals(input, cachedInput, StringComparison.OrdinalIgnoreCase))
                RebuildOptions(input);
            else if (blinkFlipped && options.Count > 0)
                RefreshLabels();
            foreach (var option in options)
                DrawOption(option, ref input);
            HandleInput(ref input);
        }

        private static void RebuildOptions (string input)
        {
            options.Clear();
            cachedInput = input;
            maxWidth = 0;
            var argCount = ConsoleGUI.CountArguments(input);
            foreach (var kv in CommandDatabase.Registered)
                if (input.Length == 1 && char.IsWhiteSpace(input[0]) ||
                    kv.Key.StartsWith(ExtractMethodName(input), StringComparison.OrdinalIgnoreCase))
                    options.Add(BuildOption(kv.Key, kv.Value, argCount));
        }

        private static string ExtractMethodName (string input)
        {
            var trimmed = input.Trim();
            var firstSpaceIndex = input.IndexOf(' ');
            return firstSpaceIndex > 0 ? trimmed.Substring(0, firstSpaceIndex - 1) : trimmed;
        }

        private static Option BuildOption (string methodName, MethodInfo info, int argCount)
        {
            // Custom hint (CommandHints.SetParameters) — shown instead of the method signature.
            var parameters = CommandHints.TryGetParameters(methodName, out var hints)
                ? hints.Select(h => $"{h.Name}: {h.Type}").ToArray()
                : info.GetParameters().Select(p => $"{p.Name}: {p.ParameterType.Name}").ToArray();
            return new Option { MethodName = methodName, Parameters = parameters, ArgCount = argCount, Label = BuildLabel(methodName, parameters, argCount) };
        }

        // Entered parameters stay bright, the one being typed blinks gray/white, the rest stay dim.
        private static string BuildLabel (string methodName, string[] parameters, int argCount)
        {
            var sb = new StringBuilder();
            sb.Append(methodName).Append(" <color=#aaa>(");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                var color = i < argCount || (i == argCount && blinkOn) ? "#fff" : "#aaa";
                sb.Append("<color=").Append(color).Append('>').Append(parameters[i]).Append("</color>");
            }
            sb.Append(")</color>");
            return sb.ToString();
        }

        private static void RefreshLabels ()
        {
            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                option.Label = BuildLabel(option.MethodName, option.Parameters, option.ArgCount);
                options[i] = option;
            }
        }

        private static void DrawOption (Option option, ref string input)
        {
            var label = new GUIContent(option.Label);
            var width = ConsoleGUI.Style.CalcSize(label).x + ConsoleGUI.Style.contentOffset.x * 2;
            if (width > maxWidth) maxWidth = width;
            var height = ConsoleGUI.Size.y;
            var y = ConsoleGUI.Size.y + ConsoleGUI.LogAreaHeight + options.IndexOf(option) * height;
            var rect = new Rect(0, y, maxWidth, height);
            if (GUI.Button(rect, label, ConsoleGUI.Style) && !input.StartsWith(option.MethodName, StringComparison.OrdinalIgnoreCase))
                input = option.MethodName;
        }

        private static void HandleInput (ref string input)
        {
            if (!Event.current.isKey) return;
            if (options.Count > 0 && Event.current.keyCode == KeyCode.Tab)
            {
                input = options[0].MethodName;
                // External text change keeps the old caret index and re-derives a selection over the
                // changed region — move the caret to the end of the completed word and clear the highlight.
                ConsoleGUI.MoveCaretToEnd(input);
            }
        }
    }
}
