/*
MIT License
Copyright (c) 2019 Laurent Kempé
https://github.com/laurentkempe/DynamicRun/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using MinecraftClient;
using SingleFileExtractor.Core;

namespace DynamicRun.Builder
{
    internal class Compiler
    {
        public CompileResult Compile(string filepath, string fileName)
        {
            ConsoleIO.WriteLogLine($"Starting compilation of: '{fileName}'");

            using var peStream = new MemoryStream();
            var result = GenerateCode(filepath, fileName).Emit(peStream);

            if (!result.Success)
            {
                ConsoleIO.WriteLogLine("Compilation done with error.");

                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                return new CompileResult()
                {
                    Assembly = null,
                    HasCompiledSucecssfully = false,
                    Failures = failures.ToList()
                };
            }

            ConsoleIO.WriteLogLine("Compilation done without any error.");

            peStream.Seek(0, SeekOrigin.Begin);

            return new CompileResult()
            {
                Assembly = peStream.ToArray(),
                HasCompiledSucecssfully = true,
                Failures = null
            };
        }

        private static CSharpCompilation GenerateCode(string sourceCode, string fileName)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var mods = Assembly.GetEntryAssembly()!.GetModules();

#pragma warning disable IL3000
            // System.Private.CoreLib
            var A = typeof(object).Assembly.Location;
            // System.Console
            var B = typeof(Console).Assembly.Location;
            // The path to MinecraftClient.dll
            var C = typeof(Program).Assembly.Location;

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(A),
                MetadataReference.CreateFromFile(B)
            };

            // We're on a Single File Application, so we need to extract the executable to get the assembly.
            if (string.IsNullOrEmpty(C))
            {
                // Create a temporary file to copy the executable to.
                var executableDir = System.AppContext.BaseDirectory;
                var executablePath = Path.Combine(executableDir, "MinecraftClient.exe");
                var tempFileName = Path.GetTempFileName();
                if (File.Exists(executablePath))
                {
                    // Copy the executable to a temporary path.
                    ExecutableReader e = new();
                    File.Delete(tempFileName);
                    File.Copy(executablePath, tempFileName);

                    // Access the contents of the executable.
                    var viewAccessor = MemoryMappedFile.CreateFromFile(tempFileName, FileMode.Open).CreateViewAccessor();
                    var manifest = e.ReadManifest(viewAccessor);
                    var files = manifest.Files;

                    Stream? assemblyStream;

                    var assemblyrefs = Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()!;
                    assemblyrefs.Add(new("MinecraftClient"));

                    foreach (var refs in assemblyrefs)
                    {
                        var loadedAssembly = Assembly.Load(refs);
                        if (string.IsNullOrEmpty(loadedAssembly.Location))
                        {
                            // Check if we can access the file from the executable.
                            var reference = files.FirstOrDefault(x => x.RelativePath.Remove(x.RelativePath.Length - 4) == refs.Name);
                            var refCount = files.Count(x => x.RelativePath.Remove(x.RelativePath.Length - 4) == refs.Name);
                            if (refCount > 1)
                            {
                                // Safety net for the case where the assembly is referenced multiple times.
                                // Should not happen normally, but we can make exceptions when it does happen.
                                throw new InvalidOperationException("Too many references to the same assembly. Assembly name: " + refs.Name);
                            }
                            if (reference == null)
                            {
                                throw new InvalidOperationException("The executable does not contain a referenced assembly. Assembly name: " + refs.Name);
                            }

                            assemblyStream = GetStreamForFileEntry(viewAccessor, reference);
                            references.Add(MetadataReference.CreateFromStream(assemblyStream!));
                            continue;
                        }
                        references.Add(MetadataReference.CreateFromFile(loadedAssembly.Location));
                    }

                    // Cleanup.
                    viewAccessor.Flush();
                    viewAccessor.Dispose();
                }
            }
            else
            {
                references.Add(MetadataReference.CreateFromFile(C));
                Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList().ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));
            }
#pragma warning restore IL3000
            return CSharpCompilation.Create($"{fileName}.dll",
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }

        private static Stream? GetStreamForFileEntry(MemoryMappedViewAccessor viewAccessor, FileEntry file)
        {
            if (typeof(BundleExtractor).GetMethod("GetStreamForFileEntry", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { viewAccessor, file }) is not Stream stream)
                throw new InvalidOperationException("The executable does not contain the assembly. Assembly name: " + file.RelativePath);

            return stream;
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