/*
MIT License
Copyright (c) 2019 Laurent Kempé
https://github.com/laurentkempe/DynamicRun/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace MinecraftClient.Scripting.DynamicRun.Builder
{
    internal class CompileRunner
    {
        public object? Execute(byte[] compiledAssembly, string[] args, Dictionary<string, object>? localVars, ChatBot apiHandler)
        {
            var assemblyLoadContextWeakRef = LoadAndExecute(compiledAssembly, args, localVars, apiHandler);

            for (var i = 0; i < 8 && assemblyLoadContextWeakRef.Item1.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            ConsoleIO.WriteLogLine(assemblyLoadContextWeakRef.Item1.IsAlive ? "[Script] Script continues to run." : "[Script] Script finished!");
            return assemblyLoadContextWeakRef.Item2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Tuple<WeakReference, object?> LoadAndExecute(byte[] compiledAssembly, string[] args, Dictionary<string, object>? localVars, ChatBot apiHandler)
        {
            using var asm = new MemoryStream(compiledAssembly);
            var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();

            var assembly = assemblyLoadContext.LoadFromStream(asm);
            var compiledScript = assembly.CreateInstance("ScriptLoader.Script")!;
            var execResult = compiledScript.GetType().GetMethod("__run")!.Invoke(compiledScript, new object[] { new CSharpAPI(apiHandler, localVars), args });

            assemblyLoadContext.Unload();

            return new(new WeakReference(assemblyLoadContext), execResult);
        }
    }
}