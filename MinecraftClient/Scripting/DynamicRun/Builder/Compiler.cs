using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using MinecraftClient;

namespace DynamicRun.Builder
{
    internal class Compiler
    {
        public CompileResult Compile(string filepath, string fileName)
        {
            Console.WriteLine($"Starting compilation of: '{fileName}'");

            using (var peStream = new MemoryStream())
            {
                var result = GenerateCode(filepath, fileName).Emit(peStream);

                if (!result.Success)
                {
                    Console.WriteLine("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    return new CompileResult() {
                        Assembly = null,
                        HasCompiledSucecssfully = false,
                        Failures = failures.ToList()
                    };
                }

                Console.WriteLine("Compilation done without any error.");

                peStream.Seek(0, SeekOrigin.Begin);

                return new CompileResult() {
                    Assembly = peStream.ToArray(),
                    HasCompiledSucecssfully = true,
                    Failures = null
                };
            }
        }

        private static CSharpCompilation GenerateCode(string sourceCode, string fileName)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ChatBot).Assembly.Location)
            };
            
            Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            return CSharpCompilation.Create($"{fileName}.dll",
                new[] { parsedSyntaxTree }, 
                references: references, 
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }

        internal struct CompileResult {
            internal byte[]? Assembly;
            internal bool HasCompiledSucecssfully;
            internal List<Diagnostic>? Failures;
            public CompileResult(bool hasCompiledSucecssfully, List<Diagnostic>? failures, byte[]? assembly) {
                HasCompiledSucecssfully = hasCompiledSucecssfully;
                Failures = failures;
                Assembly = assembly;
            }
        }
    }
}