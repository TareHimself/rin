using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class ComponentRegistrationGeneratorTests
{
    private static readonly MetadataReference[] References = ((string)AppContext
            .GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToArray();

    private const string RinWorldStubs = @"
namespace Rin.World
{
    public interface IComponentData { }
    public interface ITagData { }
    public interface IDerivedComponentData : IComponentData { }

    public static class ComponentRegistry
    {
        public static void RegisterComponent<T>() where T : struct, IComponentData { }
        public static void RegisterTag<T>() where T : struct, ITagData { }
    }
}
";

    private static (GeneratorDriverRunResult result, Compilation output) Run(params string[] sources)
    {
        var compilation = CSharpCompilation.Create(nameof(ComponentRegistrationGeneratorTests),
            sources.Select(source => CSharpSyntaxTree.ParseText(source)), References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(new ComponentRegistrationGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    private static string GeneratedText(GeneratorDriverRunResult result) =>
        result.GeneratedTrees.Single().GetText().ToString();

    [Fact]
    public void RegistersComponentsAndTagsInAModuleInitializer()
    {
        const string source = @"
using Rin.World;

namespace Game;

public struct Health : IComponentData { public int Value; }
internal record struct Speed(float Value) : IComponentData;
public struct Proxy : IDerivedComponentData { public int Handle; }
public struct Stunned : ITagData;
public partial struct Split : IComponentData;
public partial struct Split : System.IEquatable<Split> { public bool Equals(Split other) => true; }

public class Holder
{
    internal struct Nested : IComponentData;
}

public struct NotAComponent : System.IDisposable { public void Dispose() { } }
public class ClassComponent : IComponentData { }
";
        var (result, output) = Run(RinWorldStubs, source);

        Assert.Empty(result.Diagnostics);
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var text = GeneratedText(result);
        Assert.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]", text);
        Assert.Contains("global::Rin.World.ComponentRegistry.RegisterComponent<global::Game.Health>();", text);
        Assert.Contains("global::Rin.World.ComponentRegistry.RegisterComponent<global::Game.Speed>();", text);
        Assert.Contains("global::Rin.World.ComponentRegistry.RegisterComponent<global::Game.Proxy>();", text);
        Assert.Contains("global::Rin.World.ComponentRegistry.RegisterTag<global::Game.Stunned>();", text);
        Assert.Contains("global::Rin.World.ComponentRegistry.RegisterComponent<global::Game.Holder.Nested>();", text);
        Assert.Single(text.Split('\n'), line => line.Contains("<global::Game.Split>"));
        Assert.DoesNotContain("NotAComponent", text);
        Assert.DoesNotContain("ClassComponent", text);
    }

    [Fact]
    public void EmitsNothingWithoutComponents()
    {
        const string source = @"
namespace Game;

public struct Plain { public int Value; }
";
        var (withStubs, _) = Run(RinWorldStubs, source);
        var (withoutRinWorld, _) = Run(source);

        Assert.Empty(withStubs.GeneratedTrees);
        Assert.Empty(withoutRinWorld.GeneratedTrees);
    }

    [Fact]
    public void ReportsComponentsItCannotRegister()
    {
        const string source = @"
using Rin.World;

namespace Game;

public struct Generic<T> : IComponentData where T : unmanaged { public T Value; }

public class Outer<T>
{
    public struct InGeneric : IComponentData;
}

public class Holder
{
    private struct Hidden : ITagData;
}

public struct Fine : IComponentData;
";
        var (result, output) = Run(RinWorldStubs, source);

        var flagged = result.Diagnostics.Where(d => d.Id == "RIN00024").Select(d => d.GetMessage()).ToArray();
        Assert.Equal(3, flagged.Length);
        Assert.Contains(flagged, message => message.Contains("Generic<T>"));
        Assert.Contains(flagged, message => message.Contains("InGeneric"));
        Assert.Contains(flagged, message => message.Contains("Hidden"));

        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains("RegisterComponent<global::Game.Fine>();", GeneratedText(result));
    }
}
