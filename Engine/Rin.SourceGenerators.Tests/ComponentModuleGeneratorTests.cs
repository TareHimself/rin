using System.Linq;
using Microsoft.CodeAnalysis;
using Rin.World;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class ComponentModuleGeneratorTests
{
    private const string Usings = @"
using Rin.World;
";

    private static GeneratorDriverRunResult Run(string source) =>
        GeneratorHarness.Run<ComponentModuleGenerator>(Usings + source, typeof(IComponentData));

    private static ComponentInfo[] ModuleOf(string source) =>
        ComponentModule.Of(GeneratorHarness.CompileAndLoad<ComponentModuleGenerator>(Usings + source, typeof(IComponentData)))!
            .Components.ToArray();

    private static ComponentInfo Single(string source) => Assert.Single(ModuleOf(source));

    [Fact]
    public void TheModuleIsSortedByFullTypeNameNotDeclarationOrder()
    {
        var module = ModuleOf(@"
namespace Zebra { public struct Alpha : IComponentData { public int Value; } }
namespace Apple { public struct Zulu : IComponentData { public int Value; } }
namespace Apple { public struct Bravo : IComponentData { public int Value; } }
");

        Assert.Equal(["Apple.Bravo", "Apple.Zulu", "Zebra.Alpha"], module.Select(info => info.Type.FullName));
    }

    [Fact]
    public void ATagHasNoSizeAndNoFields()
    {
        var tag = Single("public struct Stunned : ITagData;");

        Assert.True(tag.IsTag);
        Assert.Equal(0, tag.Size);
        Assert.Empty(tag.Fields);
    }

    [Fact]
    public void OnlyPublicInstanceFieldsBecomeFields()
    {
        var info = Single(@"
public struct Mixed : IComponentData
{
    public int Visible;
    public readonly float ReadOnlyVisible;
    internal int Internal;
    private int _private;
    public static int Static;
    public const int Constant = 1;
    public int Property { get; set; }
}
");

        Assert.Equal(["Visible", "ReadOnlyVisible"], info.Fields.Select(field => field.Name));
        Assert.Equal([typeof(int), typeof(float)], info.Fields.Select(field => field.Type));
    }

    [Fact]
    public void FieldOffsetsFollowTheStructLayoutIncludingPadding()
    {
        var info = Single(@"
public struct Padded : IComponentData
{
    public byte First;
    public long Wide;
    public byte Second;
    public int Last;
}
");

        Assert.Equal(24, info.Size);
        Assert.Equal([0, 8, 16, 20], info.Fields.Select(field => field.Offset));
    }

    [Fact]
    public void ANestedComponentInAVisibleTypeIsInTheModuleUnderItsRuntimeFullName()
    {
        var info = Single(@"
namespace Game { public class Outer { internal struct Inner : IComponentData { public int Value; } } }
");

        Assert.Equal("Game.Outer+Inner", info.Type.FullName);
    }

    [Fact]
    public void AGenericComponentIsLeftOutWithAWarning()
    {
        var runResult = Run("public struct Boxed<T> : IComponentData where T : unmanaged { public T Value; }");

        var diagnostic = Assert.Single(runResult.Diagnostics);
        Assert.Equal("RIN00024", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Empty(runResult.GeneratedTrees);
    }

    [Fact]
    public void APrivateNestedComponentIsLeftOutWithAnInfo()
    {
        var runResult = Run("public class Outer { private struct Hidden : IComponentData { public int Value; } }");

        var diagnostic = Assert.Single(runResult.Diagnostics);
        Assert.Equal("RIN00025", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Empty(runResult.GeneratedTrees);
    }

    [Fact]
    public void AManagedComponentIsAnError()
    {
        var runResult = Run("public struct Named : IComponentData { public string Name; }");

        Assert.Equal("RIN00026", Assert.Single(runResult.Diagnostics).Id);
    }

    [Fact]
    public void ATagWithFieldsIsAnError()
    {
        var runResult = Run("public struct Heavy : ITagData { public int Value; }");

        Assert.Equal("RIN00027", Assert.Single(runResult.Diagnostics).Id);
    }

    [Fact]
    public void AnAssemblyWithoutComponentsGetsNoModule()
    {
        var runResult = Run("public struct Plain { public int Value; }");

        Assert.Empty(runResult.Diagnostics);
        Assert.Empty(runResult.GeneratedTrees);
    }
}
