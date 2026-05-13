using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceToAI.Tests.Support;

/// <summary>
/// Erzeugt eine kleine echte .NET-DLL per Roslyn (für Decompiler-Integrationstests).
/// </summary>
internal static class MinimalTestAssemblyCompiler
{
    internal static string EmitClassLibraryDll(string outputDirectory, string assemblyName)
    {
        Directory.CreateDirectory(outputDirectory);

        var tree = CSharpSyntaxTree.ParseText(
            """
            namespace SourceToAiTestEmit;

            public static class EmitMarker
            {
                public static int Answer() => 42;
            }
            """,
            path: "EmitMarker.cs");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var dllPath = Path.Combine(outputDirectory, assemblyName + ".dll");
        var emitResult = compilation.Emit(dllPath);
        if (!emitResult.Success)
        {
            var errors = string.Join(
                "; ",
                emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException("Roslyn-Emit fehlgeschlagen: " + errors);
        }

        return dllPath;
    }
}
