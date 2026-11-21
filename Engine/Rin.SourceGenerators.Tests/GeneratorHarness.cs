using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Rin.SourceGenerators.Tests;

internal static class GeneratorHarness
{
    // The full platform set (not just System.Private.CoreLib) is needed so custom attribute
    // base-type resolution doesn't fail with CS0012.
    public static readonly MetadataReference[] PlatformReferences = ((string)AppContext
            .GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToArray();

    public static GeneratorDriverRunResult Run<TGenerator>(string source, params Type[] referencedAssembliesOf)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = CSharpCompilation.Create(typeof(TGenerator).Name, [CSharpSyntaxTree.ParseText(source)],
            ReferencesTo(referencedAssembliesOf));
        return CSharpGeneratorDriver.Create(new TGenerator()).RunGenerators(compilation).GetRunResult();
    }

    /// <summary>
    /// Compiles <paramref name="source" /> with the generator's output and loads it, for tests that run generated code.
    /// </summary>
    public static Assembly CompileAndLoad<TGenerator>(string source, params Type[] referencedAssembliesOf)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = CSharpCompilation.Create($"{typeof(TGenerator).Name}_{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(source)], ReferencesTo(referencedAssembliesOf),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CSharpGeneratorDriver.Create(new TGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var generated, out _);

        using var image = new MemoryStream();
        var emitted = generated.Emit(image);
        if (!emitted.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        return Assembly.Load(image.ToArray());
    }

    private static IEnumerable<MetadataReference> ReferencesTo(Type[] referencedAssembliesOf) =>
        PlatformReferences.Concat(referencedAssembliesOf.Select(type =>
            (MetadataReference)MetadataReference.CreateFromFile(type.Assembly.Location)));
}
