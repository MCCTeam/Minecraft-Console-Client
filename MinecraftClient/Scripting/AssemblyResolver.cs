using System;
using System.Collections.Generic;
using System.Reflection;

namespace MinecraftClient.Scripting;

public static class AssemblyResolver
{
    private static Dictionary<string, string> ScriptAssemblies = new();
    static AssemblyResolver()
    {
        // Manually resolve assemblies that .NET can't resolve automatically.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var asmReqName = new AssemblyName(args.Name);

            // Check the script-referenced assemblies if we have the DLL that is required.
            foreach (var dll in ScriptAssemblies)
            {
                // If we have the assembly, load it.
                if (asmReqName.FullName == dll.Key)
                {
                    return Assembly.LoadFile(dll.Value);
                }
            }

            ConsoleIO.WriteLogLine($"[Script Error] Failed to resolve assembly {args.Name} (are you missing a DLL file?)");
            return null;
        };
    }

    internal static void AddAssembly(string AssemblyFullName, string AssemblyPath)
    {
        if (ScriptAssemblies.ContainsKey(AssemblyFullName))
            return;

        ScriptAssemblies.Add(AssemblyFullName, AssemblyPath);
    }
}