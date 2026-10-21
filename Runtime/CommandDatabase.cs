using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityConsole
{
    internal static class CommandDatabase
    {
        public static IReadOnlyDictionary<string, MethodInfo> Registered => methodInfoCache;

        private static Dictionary<string, MethodInfo> methodInfoCache;

        public static void ExecuteCommand (string methodName, params string[] args)
        {
            if (methodInfoCache == null || !methodInfoCache.ContainsKey(methodName))
            {
                Debug.LogWarning($"UnityConsole: Command `{methodName}` is not registered in the database.");
                return;
            }

            // Built-in commands with optional arguments bypass the strict arity check below.
            if (string.Equals(methodName, BuiltInCommands.HelpAlias, StringComparison.OrdinalIgnoreCase))
            {
                BuiltInCommands.Dispatch(args);
                return;
            }

            var methodInfo = methodInfoCache[methodName];
            var parametersInfo = methodInfo.GetParameters();
            if (parametersInfo.Length != args.Length)
            {
                Debug.LogWarning($"UnityConsole: Command `{methodName}` requires {parametersInfo.Length} args, while {args.Length} were provided.");
                return;
            }
            var parameters = new object[parametersInfo.Length];
            for (int i = 0; i < args.Length; i++)
                parameters[i] = Convert.ChangeType(args[i], parametersInfo[i].ParameterType, System.Globalization.CultureInfo.InvariantCulture);
            methodInfo.Invoke(null, parameters);
        }

        internal static void RegisterCommands (Dictionary<string, MethodInfo> commands = null)
        {
            methodInfoCache = commands != null
                ? new Dictionary<string, MethodInfo>(commands, StringComparer.OrdinalIgnoreCase)
                : AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly => !assembly.IsDynamic)
                    .SelectMany(assembly => assembly.GetExportedTypes())
                    .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    .Where(method => method.GetCustomAttribute<ConsoleCommandAttribute>() != null)
                    .ToDictionary(method => method.GetCustomAttribute<ConsoleCommandAttribute>().Alias ?? method.Name, StringComparer.OrdinalIgnoreCase);

            // Reserved aliases belong to the package's built-in commands — project code cannot take them.
            foreach (var alias in BuiltInCommands.ReservedAliases)
            {
                if (methodInfoCache.ContainsKey(alias))
                {
                    Debug.LogError($"UnityConsole: command `{alias}` is reserved by the package's built-in commands — rename your [ConsoleCommand] method or its alias.");
                    methodInfoCache.Remove(alias);
                }
            }

            foreach (var kv in BuiltInCommands.All)
                methodInfoCache[kv.Key] = kv.Value;
        }
    }
}
