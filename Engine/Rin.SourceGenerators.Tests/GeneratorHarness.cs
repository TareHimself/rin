using System;
using System.IO;
using System.Linq;
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
        var references = PlatformReferences.Concat(referencedAssembliesOf.Select(type =>
            (MetadataReference)MetadataReference.CreateFromFile(type.Assembly.Location)));
        var compilation = CSharpCompilation.Create(typeof(TGenerator).Name, [CSharpSyntaxTree.ParseText(source)],
            references);
        return CSharpGeneratorDriver.Create(new TGenerator()).RunGenerators(compilation).GetRunResult();
    }
}
