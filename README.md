## Description

Allows executing static C# methods via an on-demand console IMGUI.

![](https://github.com/elringus/unity-console/blob/main/preview.gif)

## Installation

Use [UPM](https://docs.unity3d.com/Manual/upm-ui-giturl.html) to install the package via the following Git URL: `https://github.com/maxx48lip/UnityBetterConsole.git#package`.

![](https://i.gyazo.com/b54e9daa9a483d9bf7f74f0e94b2d38a.gif)

Alternatively, manually copy the `Runtime` folder and `package.json` from the repository to the Unity project.

Minimum supported Unity version: 2019.4.

## How to Use

Register commands by adding `ConsoleCommand` attribute to static C# methods. The attribute has an optional string argument, allowing to assign an alias (shortcut).

```csharp
[UnityConsole.ConsoleCommand("hello")]
public static void PrintHelloWorld () => Debug.Log("Hello World!");

[UnityConsole.ConsoleCommand]
public static void Add (int arg1, int arg2) => Debug.Log(arg1 + arg2);
```

Enable the console at runtime with:

```csharp
UnityConsole.ConsoleGUI.Initialize()
```

Toggle console GUI with `~` key. The key can be overridden via `ConsoleGUI.ToggleKey` public static property. It's also possible to toggle console with a multi-(3 or more) touch on touch screen devices.

In the console, type either method name or alias of a registered command and press `Enter` key to invoke the method. Method arguments are separated with a single whitespace. To specify string arguments with whitespace, wrap them in double or single quotes.

Use `Up` and `Down` to navigate over previously executed commands. Press `Tab` to complete the current input against registered command aliases.

The console stays open after executing a command; close it with `ESC`. The input field is focused automatically when the console opens, so you can start typing right away.

---

The console ships with two built-in commands:

- `help [page]` — lists all registered commands alphabetically, 20 per page
- `clear` — empties the on-screen log buffer

Their aliases are reserved and cannot be re-used by project code.

To disable the console at runtime:

```csharp
UnityConsole.ConsoleGUI.Destroy()
```

## Log Output

The console keeps its own on-screen log buffer (up to 200 entries). Write to it with the `ConsoleLog` class — each entry is also forwarded to the corresponding `Debug` method, while regular `Debug.Log` output is not captured.

```csharp
UnityConsole.ConsoleLog.Info("Player saved");
UnityConsole.ConsoleLog.Warning("Low memory");
UnityConsole.ConsoleLog.Error("Failed to load scene");
```

The buffer shows 20 lines at a time; use the up/down buttons in the console to page through older entries. The number of visible lines can be changed via `ConsoleGUI.LogLines`.

Entries support colored segments: a color segment sets the color for all following text, while text before any color is drawn with the level default color.

```csharp
using UnityConsole;

ConsoleLog.Info("plain ", ConsoleTextColor.Orange, "orange tail");
```

Available colors: Orange, Magenta, LightBlue, Yellow, Lime, Pink, Toxic, LightGray, Cyan, Purple, Blue, Brown, Green, Red, Lavender and White.

## Preprocessors

It's possible to inject delegates to modify the console input before it's send for execution, eg:

```csharp
using UnityConsole;
using UnityEngine;

public class TestPreprocessor : MonoBehaviour
{
    private void OnEnable ()
    {
        InputPreprocessor.AddPreprocessor(PreprocessInput);
    }

    private void OnDisable ()
    {
        InputPreprocessor.RemovePreprocessor(PreprocessInput);
    }

    private string PreprocessInput (string input)
    {
        if (input != null && input.StartsWith("@"))
        {
            Debug.Log(input);
            return null;
        }
        return input;
    }
}
```

— will intercept commands starting with `@` and instead log the input. Notice, that when a preprocessor delegate returns `null` it will stop the default console behaviour.

## Custom Hints

By default, `help` and Tab autocomplete show a command's parameters from its method signature (`name: Type`). When the actual signature differs from what is convenient to display — for example, a variable number of arguments packed into one string parameter — register custom parameter hints for the command alias:

```csharp
UnityConsole.CommandHints.SetParameters("createRuin",
    ("x", "Int32"), ("y", "Int32"), ("n", "Int32"), ("level", "Int32"),
    ("scavenger", "Int32"), ("melee", "Int32"));
```

The hint replaces the signature-derived list for that command: `help` will show `createRuin <x:Int32> <y:Int32> ...`, and autocomplete will suggest `createRuin (x: Int32, y: Int32, ...)`. Commands without a registered hint keep showing their method signature.