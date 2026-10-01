using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.Shade.Tests;

internal static class FixtureSource
{
    public static string Read(string relativePath, [CallerFilePath] string callerPath = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(callerPath)!, relativePath));
}

internal static class CompilationBuilder
{
    // The full set of reference assemblies (not just System.Private.CoreLib) is needed so custom
    // attribute base-type resolution (e.g. ShaderAttribute -> Attribute) doesn't fail with CS0012.
    private static readonly MetadataReference[] References = ((string)AppContext
            .GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(Shader).Assembly.Location))
        .ToArray();

    public static Compilation Build(string source, string name = "RinShadeTests") =>
        Build([source], name);

    public static Compilation Build(string[] sources, string name = "RinShadeTests")
    {
        return CSharpCompilation.Create(name,
            sources.Select(s => CSharpSyntaxTree.ParseText(s)),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
