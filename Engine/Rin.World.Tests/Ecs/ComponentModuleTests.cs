using System.Numerics;
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
    public void TheEngineModuleGivesEachPublicFieldItsOffset()
    {
        ComponentInfo? localTransform = null;
        foreach (var info in ComponentModule.Engine.Components)
            if (info.Type == typeof(LocalTransform)) localTransform = info;

        Assert.That(localTransform!.Fields, Is.EqualTo(new ComponentField[]
        {
            new(nameof(LocalTransform.Position), typeof(Vector3), 0),
            new(nameof(LocalTransform.Rotation), typeof(Quaternion), 12),
            new(nameof(LocalTransform.Scale), typeof(Vector3), 28)
        }));
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
