using System.Numerics;
using Rin.World.Graphics;
using Rin.World.Physics;
using Rin.World.Transforms;

namespace Rin.World.Tests.Ecs;

public class ComponentModuleTests
{
    [Test]
    public void TheEngineModuleHoldsEveryRinWorldComponentAndTagSortedByFullName()
    {
        var expected = typeof(World).Assembly.GetTypes()
            .Where(type => type.IsValueType && typeof(IComponentData).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal);
        var actual = new List<string?>();
        foreach (var info in ComponentModule.Engine.Components) actual.Add(info.Type.FullName);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void TheEngineModuleGivesEachFieldItsKindAndOffset()
    {
        var fields = InfoOf<LocalTransform>().Fields;

        Assert.That(fields.Select(field => (field.Name, field.Type.Kind, field.Offset)), Is.EqualTo(new[]
        {
            (nameof(LocalTransform.Position), FieldKind.Vector3, 0),
            (nameof(LocalTransform.Rotation), FieldKind.Quaternion, 12),
            (nameof(LocalTransform.Scale), FieldKind.Vector3, 28)
        }));
    }

    [Test]
    public void MeshHandlesAreProcessLocal()
    {
        var fields = InfoOf<StaticMesh>().Fields;

        Assert.That(fields.Select(field => (field.Name, field.IsProcessLocal)), Is.EqualTo(new[]
        {
            (nameof(StaticMesh.Mesh), true),
            (nameof(StaticMesh.Materials), true)
        }));
    }

    [Test]
    public void AComposedSchemaListsEveryFieldTypeOnce()
    {
        var vector3 = ComponentSchema.Engine.FieldTypeOf(typeof(Vector3));

        Assert.That(vector3!.Kind, Is.EqualTo(FieldKind.Vector3));
        Assert.That(InfoOf<LocalTransform>().Fields[0].Type, Is.SameAs(vector3));
        Assert.That(ComponentSchema.Engine.FieldTypes, Is.Unique);
    }

    [Test]
    public void RenamingAFieldChangesTheLayoutHash()
    {
        var info = InfoOf<Velocity>();
        var renamed = ComponentInfo.Of<Velocity>([info.Fields[0] with { Name = "Speed" }, info.Fields[1]], []);

        Assert.That(renamed.LayoutHash(), Is.Not.EqualTo(info.LayoutHash()));
    }

    private static ComponentInfo InfoOf<T>() where T : unmanaged
    {
        foreach (var info in ComponentModule.Engine.Components)
            if (info.Type == typeof(T)) return info;
        throw new InvalidOperationException($"{typeof(T).Name} is not in the engine module.");
    }

    [Test]
    public void OfFindsTheModuleGeneratedForAnAssembly()
    {
        var testModule = ComponentModule.Of(typeof(ComponentModuleTests).Assembly);
        var testTypes = new List<Type>();
        foreach (var info in testModule!.Components) testTypes.Add(info.Type);

        Assert.That(ComponentModule.Of(typeof(World).Assembly), Is.SameAs(ComponentModule.Engine));
        Assert.That(testTypes, Does.Contain(typeof(Health)).And.Contain(typeof(Stunned)));
        Assert.That(ComponentModule.Of(typeof(object).Assembly), Is.Null);
    }
}
