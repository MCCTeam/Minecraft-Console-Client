/*
MIT License
Copyright (c) 2019 Laurent Kempé
https://github.com/laurentkempe/DynamicRun/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SingleFileExtractor.Core;

namespace MinecraftClient.Scripting.DynamicRun.Builder
{
    internal class Compiler
    {
        private static readonly object s_baseReferencesLock = new();
        private static List<MetadataReference>? s_baseReferences;

        public CompileResult Compile(string filepath, string fileName, List<string> additionalAssemblies)
        {
            using var peStream = new MemoryStream();
            var result = GenerateCode(filepath, fileName, additionalAssemblies).Emit(peStream);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                return new CompileResult()
                {
                    Assembly = null,
                    HasCompiledSucecssfully = false,
                    Failures = failures.ToList()
                };
            }

            peStream.Seek(0, SeekOrigin.Begin);

            return new CompileResult()
            {
                Assembly = peStream.ToArray(),
                HasCompiledSucecssfully = true,
                Failures = null
            };
        }

        private static CSharpCompilation GenerateCode(string sourceCode, string fileName, List<string> additionalAssemblies)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>(GetBaseReferences());

            // Find if any additional assembly DLL exists in the base directory where the .exe exists.
            foreach (var assembly in additionalAssemblies)
            {
                var dllPath = Path.Combine(AppContext.BaseDirectory, assembly);
                if (File.Exists(dllPath))
                {
                    references.Add(MetadataReference.CreateFromFile(dllPath));
                    // Store the reference in our Assembly Resolver for future reference.
                    AssemblyResolver.AddAssembly(Assembly.LoadFile(dllPath).FullName!, dllPath);
                }
                else
                {
                    ConsoleIO.WriteLogLine($"[Script Error] {assembly} is defined in script, but cannot find DLL! Script may not run.");
                }
            }

            return CSharpCompilation.Create($"{fileName}.dll",
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }

        private static List<MetadataReference> GetBaseReferences()
        {
            lock (s_baseReferencesLock)
            {
                if (s_baseReferences is not null)
                    return s_baseReferences;

                var references = new List<MetadataReference>();

#pragma warning disable IL3000 // We determine if we are in a self-contained binary by checking specifically if the Assembly file path is null.

                var SystemPrivateCoreLib = typeof(object).Assembly.Location;    // System.Private.CoreLib
                var SystemConsole = typeof(Console).Assembly.Location;          // System.Console
                var MinecraftClientDll = typeof(Program).Assembly.Location;     // The path to MinecraftClient.dll

                // We're on a self-contained binary, so we need to extract the executable to get the assemblies.
                if (string.IsNullOrEmpty(MinecraftClientDll))
                {
                    // Create a temporary file to copy the executable to.
                    var executablePath = Environment.ProcessPath;
                    if (executablePath is null)
                        throw new InvalidOperationException("Cannot determine the process path for self-contained scripting extraction.");
                    var tempPath = Path.Combine(Path.GetTempPath(), "mcc-scripting");
                    Directory.CreateDirectory(tempPath);

                    var tempFile = Path.Combine(tempPath, "mcc-executable");
                    var useExisting = false;

                    // Check if we already have the executable in the temporary path.
                    foreach (var file in Directory.EnumerateFiles(tempPath))
                    {
                        if (file.EndsWith("mcc-executable"))
                        {
                            // Check if the file is the same as the current executable without loading both into LOH.
                            if (FilesAreEqual(file, executablePath))
                            {
                                useExisting = true;
                                break;
                            }

                            // If not, refresh the cache.
                            File.Delete(file);
                            break;
                        }
                    }

                    if (!File.Exists(executablePath))
                    {
                        throw new FileNotFoundException("[Script Error] Could not locate the current folder of MCC for scripting.");
                    }

                    // Copy the executable to a temporary path.
                    if (!useExisting)
                        File.Copy(executablePath, tempFile);

                    // Access the contents of the executable.
                    using ExecutableReader executableReader = new(tempFile);
                    if (!executableReader.IsSingleFile)
                        throw new InvalidOperationException("[Script Error] The executable is not a single-file bundle.");

                    var files = executableReader.Bundle.Files;

                    var assemblyrefs = Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()!;
                    assemblyrefs.Add(new("MinecraftClient"));
                    assemblyrefs.Add(new("System.Private.CoreLib"));
                    // Facade assemblies needed for compiling scripts that reference netstandard libraries (e.g. Brigadier.NET)
                    assemblyrefs.Add(new("netstandard"));
                    assemblyrefs.Add(new("System.Runtime"));
                    assemblyrefs.Add(new("System.Private.Uri"));
                    assemblyrefs.Add(new("System.Net.Requests"));
                    assemblyrefs.Add(new("System.Net.WebSockets"));
                    assemblyrefs.Add(new("System.Net.HttpListener"));
                    assemblyrefs.Add(new("System.Net.Primitives"));
                    assemblyrefs.Add(new("System.Net.Sockets"));
                    assemblyrefs.Add(new("Microsoft.Win32.Primitives"));
                    assemblyrefs.Add(new("System.Collections.Concurrent"));

                    foreach (var refs in assemblyrefs)
                    {
                        Assembly? loadedAssembly;
                        try
                        {
                            loadedAssembly = Assembly.Load(refs);
                        }
                        catch (FileNotFoundException)
                        {
                            // Facade assemblies like netstandard may not be loadable in all environments
                            continue;
                        }

                        if (string.IsNullOrEmpty(loadedAssembly.Location))
                        {
                            // Check if we can access the file from the executable.
                            var reference = files.FirstOrDefault(x =>
                                Path.GetFileNameWithoutExtension(x.RelativePath) == refs.Name);
                            var refCount = files.Count(x => Path.GetFileNameWithoutExtension(x.RelativePath) == refs.Name);
                            if (refCount > 1)
                            {
                                // Safety net for the case where the assembly is referenced multiple times.
                                // Should not happen normally, but we can make exceptions when it does happen.
                                throw new InvalidOperationException(
                                    "[Script Error] Too many references to the same assembly. Assembly name: " + refs.Name);
                            }

                            if (reference is null)
                            {
                                // Facade assemblies may not be in the bundle - skip them silently
                                continue;
                            }

                            using Stream assemblyStream = reference.AsStream();
                            references.Add(MetadataReference.CreateFromStream(assemblyStream));
                            continue;
                        }

                        references.Add(MetadataReference.CreateFromFile(loadedAssembly.Location));
                    }
                }
                else
                {
                    references.Add(MetadataReference.CreateFromFile(SystemPrivateCoreLib));
                    references.Add(MetadataReference.CreateFromFile(SystemConsole));
                    references.Add(MetadataReference.CreateFromFile(MinecraftClientDll));
                    Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList().ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

                    // Add facade assemblies needed for Roslyn compilation when referencing
                    // libraries that target netstandard (e.g. Brigadier.NET).
                    var runtimeDir = Path.GetDirectoryName(SystemPrivateCoreLib)!;
                    foreach (var facadeName in new[] { "netstandard.dll", "System.Runtime.dll", "System.Private.Uri.dll", "System.Net.Requests.dll",
                        "System.Net.WebSockets.dll", "System.Net.HttpListener.dll", "System.Net.Primitives.dll", "System.Net.Sockets.dll",
                        "Microsoft.Win32.Primitives.dll", "System.Collections.Concurrent.dll" })
                    {
                        var facadePath = Path.Combine(runtimeDir, facadeName);
                        if (File.Exists(facadePath))
                            references.Add(MetadataReference.CreateFromFile(facadePath));
                    }
                }
#pragma warning restore IL3000

                s_baseReferences = references;
                return references;
            }
        }

        private static bool FilesAreEqual(string path1, string path2)
        {
            FileInfo info1 = new(path1);
            FileInfo info2 = new(path2);
            if (!info1.Exists || !info2.Exists || info1.Length != info2.Length)
                return false;

            using FileStream fs1 = new(path1, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            using FileStream fs2 = new(path2, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
            Span<byte> buf1 = new byte[65536];
            Span<byte> buf2 = new byte[65536];
            while (true)
            {
                int r1 = fs1.Read(buf1);
                int r2 = fs2.Read(buf2);
                if (r1 != r2)
                    return false;
                if (r1 == 0)
                    return true;
                if (!buf1[..r1].SequenceEqual(buf2[..r2]))
                    return false;
            }
        }

        internal struct CompileResult
        {
            internal byte[]? Assembly;
            internal bool HasCompiledSucecssfully;
            internal List<Diagnostic>? Failures;
            public CompileResult(bool hasCompiledSucecssfully, List<Diagnostic>? failures, byte[]? assembly)
            {
                HasCompiledSucecssfully = hasCompiledSucecssfully;
                Failures = failures;
                Assembly = assembly;
            }
        }
    }
}
