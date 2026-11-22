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
    public void EveryEcsRequiresBecomesARequirementInDeclarationOrder()
    {
        var module = ModuleOf(@"
public struct Transform : IComponentData { public float X; }
public struct Collider : IComponentData;
[EcsRequires<Transform>, EcsRequires<Collider>]
public struct Body : IComponentData { public float Mass; }
[EcsRequires<Body>]
public struct Grounded : ITagData;
");

        Assert.Equal(["Transform", "Collider"], module.Single(info => info.Name == "Body").Requires.Select(type => type.Name));
        Assert.Equal(["Body"], module.Single(info => info.Name == "Grounded").Requires.Select(type => type.Name));
        Assert.Empty(module.Single(info => info.Name == "Transform").Requires);
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
    public void EveryInstanceFieldBecomesAFieldWithAutoPropertiesUnderTheirPropertyName()
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
    public long Property { get; set; }
}
");

        Assert.Equal(["Visible", "ReadOnlyVisible", "Internal", "_private", "Property"], info.Fields.Select(field => field.Name));
        Assert.Equal([FieldKind.Int32, FieldKind.Float32, FieldKind.Int32, FieldKind.Int32, FieldKind.Int64],
            info.Fields.Select(field => field.Type.Kind));
        Assert.Equal([0, 4, 8, 12, 16], info.Fields.Select(field => field.Offset));
    }

    [Fact]
    public void APositionalRecordStructIsDescribedByItsParameters()
    {
        var info = Single("public record struct Health(int Current, float Regen) : IComponentData;");

        Assert.Equal(["Current", "Regen"], info.Fields.Select(field => field.Name));
        Assert.Equal([0, 4], info.Fields.Select(field => field.Offset));
    }

    [Fact]
    public void KnownTypesGetTheirOwnKind()
    {
        var info = Single(@"
public enum Mode { Off, On }
public struct Known : IComponentData
{
    public bool Flag;
    public double Wide;
    public System.Numerics.Vector3 Position;
    public System.Numerics.Quaternion Rotation;
    public Mode Mode;
    public Entity Target;
}
");

        Assert.Equal(
            [FieldKind.Bool, FieldKind.Float64, FieldKind.Vector3, FieldKind.Quaternion, FieldKind.Enum, FieldKind.Entity],
            info.Fields.Select(field => field.Type.Kind));
    }

    [Fact]
    public void ANestedStructIsDescribedByItsOwnFieldsAndSharedByEveryFieldOfItsType()
    {
        var module = ModuleOf(@"
public struct Range { public float Min; public float Max; }
public struct Bounds : IComponentData { public Range X; public Range Y; }
public struct Spread : IComponentData { public byte Pad; public Range Angle; }
");
        var bounds = module.Single(info => info.Name == "Bounds");
        var spread = module.Single(info => info.Name == "Spread");
        var range = bounds.Fields[0].Type;

        Assert.Equal(FieldKind.Struct, range.Kind);
        Assert.Equal(8, range.Size);
        Assert.Equal([("Min", 0), ("Max", 4)], range.Fields.Select(field => (field.Name, field.Offset)));
        Assert.Same(range, bounds.Fields[1].Type);
        Assert.Same(range, spread.Fields[1].Type);
        Assert.Equal(4, spread.Fields[1].Offset);
    }

    [Fact]
    public void AProcessLocalFieldOfAnUnknownTypeIsOpaque()
    {
        var info = Single(@"
public struct Handle { private ulong _data; private System.IntPtr _native; }
public struct Drawn : IComponentData
{
    [EcsProcessLocal] public Handle Mesh;
    [EcsProcessLocal] public int Slot;
    public float Scale;
}
");

        Assert.Equal([FieldKind.Opaque, FieldKind.Int32, FieldKind.Float32], info.Fields.Select(field => field.Type.Kind));
        Assert.Equal([true, true, false], info.Fields.Select(field => field.IsProcessLocal));
    }

    [Fact]
    public void DerivedFieldsAndComponentsAreMarked()
    {
        var module = ModuleOf(@"
[EcsDerived] public struct Computed : IComponentData { public float Value; }
public struct Mixed : IComponentData
{
    public float Authored;
    [EcsDerived] public float Cached;
    [field: EcsDerived] public float CachedProperty { get; internal set; }
}
");
        var mixed = module.Single(info => info.Name == "Mixed");

        Assert.True(module.Single(info => info.Name == "Computed").IsDerived);
        Assert.False(mixed.IsDerived);
        Assert.Equal([false, true, true], mixed.Fields.Select(field => field.IsDerived));
    }

    [Theory]
    [InlineData("public System.IntPtr Native;", "Native")]
    [InlineData("public char Letter;", "Letter")]
    [InlineData("public float? Radius;", "Radius")]
    [InlineData("public Inner Nested; public struct Inner { public System.UIntPtr Raw; }", "Nested.Raw")]
    public void AFieldWithNoKnownTypeIsAnErrorNamingItsPath(string members, string path)
    {
        var runResult = Run("public struct Bad : IComponentData { " + members + " }");

        var diagnostic = Assert.Single(runResult.Diagnostics);
        Assert.Equal("RIN00028", diagnostic.Id);
        Assert.Contains("'" + path + "'", diagnostic.GetMessage());
        Assert.Empty(runResult.GeneratedTrees);
    }

    [Fact]
    public void ANotSavedComponentIsNotChecked()
    {
        var info = Single("[EcsNotSaved] public struct Native : IComponentData { public System.IntPtr Pointer; }");

        Assert.False(info.IsSaved);
        Assert.Empty(info.Fields);
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
