using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityConsole
{
    public class ConsoleGUI : MonoBehaviour
    {
        // To prevent garbage when the console is hidden.
        private class OnGUIProxy : MonoBehaviour
        {
            public Action OnGUIDelegate;
            private void OnGUI () => OnGUIDelegate();
        }

        /// <summary>
        /// The key to toggle console visibility.
        /// </summary>
        public static KeyCode ToggleKey { get; set; } = KeyCode.BackQuote;
        /// <summary>
        /// Whether to toggle console when multi-(3 or more) touch is detected.
        /// </summary>
        public static bool ToggleByMultitouch { get; set; } = true;
        /// <summary>
        /// Whether to scale the console UI based on screen resolution.
        /// </summary>
        public static bool ScaleByResolution { get; set; } = true;
        /// <summary>
        /// The font to use throughout the console GUI.
        /// </summary>
        public static Font Font { get; set; }
        /// <summary>
        /// Color of the console underlay.
        /// </summary>
        public static Color BackgroundColor { get; set; } = new Color(0, 0, 0, .65f);
        /// <summary>
        /// Whether to show auto-complete list of available commands when typing in console.
        /// </summary>
        public static bool ShowCompleteList { get; set; } = true;
        /// <summary>
        /// Number of recent ConsoleLog lines shown above the input bar. 0 hides the log area.
        /// The full history (ConsoleLog.MaxEntries) is reachable with the up/down buttons next to EXECUTE.
        /// </summary>
        public static int LogLines { get; set; } = 20;

        internal static Vector2 Size { get; private set; }
        internal static Vector2 Scale { get; private set; }
        internal static GUIStyle Style { get; private set; }
        internal static float LogAreaHeight { get; private set; }

        private static GUIStyle timestampStyle;
        private static GUIStyle lineUnderlayStyle;
        private static GUIStyle[] messageStyles;
        private static GUIStyle[] colorStyles;
        private static float timestampWidth;

        private const string inputControlName = "input";
        private static ConsoleGUI instance;
        private static readonly Regex regex = new Regex("'(.+?)'|\"(.+?)\"|([^ ]+)", RegexOptions.Compiled);
        private static readonly List<string> inputBuffer = new List<string>();
        private static OnGUIProxy guiProxy;
        private static Rect consoleRect, logUpButtonRect, logDownButtonRect, execButtonRect, closeButtonRect;
        private static string input;
        private static int inputBufferIndex;
        private static int logScrollOffset;

        public static void Initialize (Dictionary<string, MethodInfo> commands = null)
        {
            if (instance) return;

            CommandDatabase.RegisterCommands(commands);

            var hostObject = new GameObject("UnityConsole");
            hostObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(hostObject);

            instance = hostObject.AddComponent<ConsoleGUI>();
            Scale = ScaleByResolution ? new Vector2(Mathf.Max(Screen.width / 1920f, 1), Mathf.Max(Screen.height / 1080f, 1)) : Vector2.one;
            // Right-side button row: [input][up][down][EXECUTE][HIDE] = 30 + 30 + 75 + 50 scaled units.
            Size = new Vector2(Screen.width - 185 * Scale.x, 25 * Scale.y);
            consoleRect = new Rect(0, 0, Size.x, Size.y);
            logUpButtonRect = new Rect(Size.x, 0, 30 * Scale.x, Size.y);
            logDownButtonRect = new Rect(Size.x + 30 * Scale.x, 0, 30 * Scale.x, Size.y);
            execButtonRect = new Rect(Size.x + 60 * Scale.x, 0, 75 * Scale.x, Size.y);
            closeButtonRect = new Rect(Screen.width - 050 * Scale.x, 0, 50 * Scale.x, Size.y);
            Style = new GUIStyle {
                richText = true,
                normal = new GUIStyleState { background = Texture2D.whiteTexture, textColor = Color.white },
                contentOffset = new Vector2(5, 5) * Scale,
                fontSize = Mathf.FloorToInt(14 * (Scale.x + Scale.y) / 2),
                font = Font
            };

            timestampStyle = new GUIStyle(Style);
            // Segment labels are drawn without their own background: each log line gets a single underlay
            // (see DrawSegments), because per-label backgrounds would overlap and cover word tails.
            messageStyles = new GUIStyle[3];
            for (int i = 0; i < messageStyles.Length; i++)
            {
                var style = new GUIStyle(Style) { richText = false };
                style.normal.background = null;
                style.normal.textColor = i == 1 ? new Color(1f, .8f, 0f) : i == 2 ? new Color(1f, .33f, .33f) : Color.white;
                messageStyles[i] = style;
            }

            colorStyles = new GUIStyle[Enum.GetValues(typeof(ConsoleTextColor)).Length];
            foreach (ConsoleTextColor color in Enum.GetValues(typeof(ConsoleTextColor)))
            {
                var style = new GUIStyle(Style) { richText = false };
                style.normal.background = null;
                style.normal.textColor = ConsoleTextColors.Get(color);
                colorStyles[(int)color] = style;
            }

            // Tinted by GUI.backgroundColor like every other control, unlike a raw DrawTexture call.
            lineUnderlayStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };

            guiProxy = hostObject.AddComponent<OnGUIProxy>();
            guiProxy.OnGUIDelegate = instance.DrawGUI;
            guiProxy.enabled = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Destroy ()
        {
            if (!instance) return;
            if (Application.isPlaying) Destroy(instance.gameObject);
            else DestroyImmediate(instance.gameObject);
        }

        public static void Show ()
        {
            guiProxy.enabled = true;
            logScrollOffset = 0;
        }

        public static void Hide () => guiProxy.enabled = false;
        // Route through Show so the input field gets focus (blinking caret, typing works immediately).
        public static void Toggle ()
        {
            if (guiProxy.enabled) Hide();
            else Show();
        }
        private void OnApplicationQuit () => Destroy();

        #if ENABLE_LEGACY_INPUT_MANAGER
        private void Update ()
        {
            if (!Application.isPlaying) return;
            if (Input.GetKeyUp(ToggleKey) || MultitouchActive())
                Toggle();
            // ESC closes the console even when the input field has focus.
            if (guiProxy.enabled && Input.GetKeyDown(KeyCode.Escape))
                Hide();

            bool MultitouchActive ()
            {
                if (Input.touchCount <= 2) return false;
                foreach (var touch in Input.touches)
                    if (touch.phase == TouchPhase.Began)
                        return true;
                return false;
            }
        }
        #endif

        private void DrawGUI ()
        {
            // ESC closes the console from any state. Handled here (not in HandleGUIInput): that method
            // only runs when GetNameOfFocusedControl matches "input", which may not hold even with a
            // blinking caret, while key events still arrive through this event stream (Tab completion works).
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                Hide();
                Event.current.Use();
            }

            GUI.backgroundColor = BackgroundColor;
            DrawLogArea();
            GUI.SetNextControlName(inputControlName);
            input = GUI.TextField(consoleRect, input, Style);
            if (GUI.Button(logUpButtonRect, "↑", Style)) ScrollLog(-1);
            if (GUI.Button(logDownButtonRect, "↓", Style)) ScrollLog(1);
            if (GUI.Button(execButtonRect, "EXECUTE", Style)) ExecuteInput();
            if (GUI.Button(closeButtonRect, "HIDE", Style)) Hide();
            if (ShowCompleteList) CompleteGUI.Draw(ref input);

            // Keep keyboard focus on the input field for as long as the console is open. IMGUI drops it
            // after Enter executes a command, and without focus the caret disappears until you click the
            // field again. No-op while already focused; on the very first frame the field may not exist
            // yet — the next frame retries.
            GUI.FocusControl(inputControlName);

            if (GUI.GetNameOfFocusedControl() == inputControlName) HandleGUIInput();
        }

        private void HandleGUIInput ()
        {
            if (inputBuffer.Count > 0 && Event.current.isKey && Event.current.keyCode == KeyCode.UpArrow)
            {
                inputBufferIndex--;
                if (inputBufferIndex < 0) inputBufferIndex = inputBuffer.Count - 1;
                input = inputBuffer[inputBufferIndex];
            }

            if (inputBuffer.Count > 0 && Event.current.isKey && Event.current.keyCode == KeyCode.DownArrow)
            {
                inputBufferIndex++;
                if (inputBufferIndex >= inputBuffer.Count) inputBufferIndex = 0;
                input = inputBuffer[inputBufferIndex];
            }

            if (Event.current.isKey && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                ExecuteInput();
                inputBuffer.Add(input);
                inputBufferIndex = 0;
                input = string.Empty;
            }
        }

        // direction: -1 scrolls toward older entries, +1 back to the newest.
        private static void ScrollLog (int direction)
        {
            int maxOffset = Math.Max(0, ConsoleLog.Count - LogLines);
            logScrollOffset = Mathf.Clamp(logScrollOffset - direction * LogLines, 0, maxOffset);
        }

        private static void DrawLogArea ()
        {
            LogAreaHeight = 0f;
            if (LogLines <= 0) return;

            var recent = ConsoleLog.GetPage(LogLines, logScrollOffset);
            if (recent.Count == 0) return;

            float lineHeight = Style.fontSize + Mathf.CeilToInt(10 * Scale.y);
            LogAreaHeight = lineHeight * recent.Count;
            var areaRect = new Rect(consoleRect.x, consoleRect.yMax, consoleRect.width, LogAreaHeight);
            if (timestampWidth == 0f)
                timestampWidth = timestampStyle.CalcSize(new GUIContent("<color=#888>00:00:00</color>")).x + Style.contentOffset.x * 3;
            GUI.BeginClip(areaRect);
            for (int i = 0; i < recent.Count; i++)
            {
                var entry = recent[i];
                float y = areaRect.height - lineHeight * (recent.Count - i);
                // Segments are drawn with plain (non-rich) styles, so angle brackets in the text are shown as-is.
                GUI.Label(new Rect(0, y, timestampWidth, lineHeight), $"<color=#888>{entry.Timestamp:HH:mm:ss}</color>", timestampStyle);
                DrawSegments(entry, new Rect(timestampWidth, y, areaRect.width - timestampWidth, lineHeight));
            }
            GUI.EndClip();
        }

        // Draws the entry segments left-to-right within rect over a single underlay for the whole line. A color
        // segment sets the style for all following text; when the line overflows it is truncated with an ellipsis
        // at the overflow point. The underlay is drawn once per line because CalcSize does not include
        // contentOffset: per-label backgrounds would start before the previous word's tail and cover it.
        private static void DrawSegments (ConsoleLog.Entry entry, Rect rect)
        {
            GUI.Box(rect, GUIContent.none, lineUnderlayStyle);

            float maxWidth = rect.width - Style.contentOffset.x * 2;
            ConsoleTextColor? currentColor = null;
            float x = 0f;

            foreach (var segment in entry.Segments)
            {
                if (segment.Color.HasValue) currentColor = segment.Color.Value;
                if (string.IsNullOrEmpty(segment.Text)) continue;

                var style = currentColor.HasValue ? colorStyles[(int)currentColor.Value] : messageStyles[(int)entry.Level];
                float width = style.CalcSize(new GUIContent(segment.Text)).x;
                if (x + width <= maxWidth)
                {
                    GUI.Label(new Rect(rect.x + x, rect.y, width, rect.height), segment.Text, style);
                    x += width;
                }
                else
                {
                    float remaining = Mathf.Max(0f, maxWidth - x);
                    string text = segment.Text;
                    while (text.Length > 1 && style.CalcSize(new GUIContent(text + "…")).x > remaining)
                        text = text.Substring(0, text.Length - 1);
                    GUI.Label(new Rect(rect.x + x, rect.y, remaining, rect.height), text + "…", style);
                    break;
                }
            }
        }

        // Number of argument tokens after the command name. Uses the same tokenization as ExecuteInput,
        // so a quoted string counts as a single argument.
        internal static int CountArguments (string input)
        {
            var count = 0;
            foreach (Match match in regex.Matches(input))
                for (int i = 1; i < match.Groups.Count; i++)
                    if (match.Groups[i].Success)
                    {
                        count++;
                        break;
                    }
            return Math.Max(0, count - 1);
        }

        private static void ExecuteInput ()
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            var preprocessedInput = InputPreprocessor.PreprocessInput(input);
            if (string.IsNullOrWhiteSpace(preprocessedInput)) return;

            var parts = new List<string>();
            foreach (Match match in regex.Matches(preprocessedInput))
                for (int i = 1; i < match.Groups.Count; i++)
                    if (match.Groups[i].Success)
                    {
                        parts.Add(match.Groups[i].Value);
                        break;
                    }

            if (parts.Count == 0) return;
            if (parts.Count == 1) CommandDatabase.ExecuteCommand(parts[0]);
            else CommandDatabase.ExecuteCommand(parts[0], parts.GetRange(1, parts.Count - 1).ToArray());
        }

        // IMGUI keeps the old caret index when a focused field's text is changed externally (Tab
        // completion), leaving the caret mid-word — and re-derives a selection range over the changed
        // region. The live state lives in an internal per-control object (UnityEngine.TextEditor inside
        // GUIStateObjects.s_StateCache): move the caret to the end of `text` and clear every selection
        // marker, then sync the previous-frame snapshot so the next pass sees no external change at all.
        // Reflection into UnityEngine internals — degrades to a no-op if the layout differs.
        internal static void MoveCaretToEnd (string text)
        {
            try
            {
                var cacheType = typeof(GUIUtility).Assembly.GetType("UnityEngine.GUIStateObjects");
                var cacheField = cacheType?.GetField("s_StateCache", BindingFlags.Static | BindingFlags.NonPublic);
                if (cacheField == null) return;
                var dict = cacheField.GetValue(null) as System.Collections.IDictionary;
                if (dict == null) return;

                const BindingFlags anyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var teType = typeof(UnityEngine.TextEditor);
                var idField = teType.GetField("controlID", anyInstance);
                var styleField = teType.GetField("style", anyInstance);
                if (idField == null || styleField == null) return;

                int focusedId = GUIUtility.keyboardControl;
                UnityEngine.TextEditor target = null;
                foreach (System.Collections.DictionaryEntry de in dict)
                {
                    if (!(de.Value is UnityEngine.TextEditor te)) continue;
                    // Prefer the currently focused field; fall back to our input field by its unique style.
                    if (focusedId != 0 && (int)idField.GetValue(te) == focusedId) { target = te; break; }
                    if (target == null && ReferenceEquals(styleField.GetValue(te), Style)) target = te;
                }
                if (target == null) return;

                int position = text.Length;
                var selObj = teType.GetField("m_TextSelecting", anyInstance).GetValue(target);
                var selType = selObj.GetType();
                // Caret at the end, no selection: clear the alt anchor and restore the "no selection" sentinel.
                SetIfPresent(selType, "m_CursorIndex", selObj, position);
                SetIfPresent(selType, "iAltCursorPos", selObj, -1);
                SetIfPresent(selType, "m_SelectIndex", selObj, int.MaxValue);
                // Sync the previous-frame snapshot: next pass must not treat this as an external text change.
                SetIfPresent(teType, "oldText", target, text);
                SetIfPresent(teType, "oldPos", target, position);
                SetIfPresent(teType, "oldSelectPos", target, position);
            }
            catch { /* dev console: caret stays where IMGUI left it */ }
        }

        static void SetIfPresent (Type type, string fieldName, object instance, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) field.SetValue(instance, value);
        }
    }
}
