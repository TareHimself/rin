using Friflo.Engine.ECS;
using Rin.World.Transforms;

namespace Rin.World.Tests.Ecs;

public class ComponentRegistryTests
{
    [Test]
    public void EveryScannedComponentAndTagIsRegistered()
    {
        var registered = new HashSet<Type>(ComponentRegistry.GetRegisteredTypes());
        var schema = EntityStore.GetEntitySchema();
        var missing = new List<string>();

        foreach (var component in schema.Components)
            if (component is not null && NeedsRegistration(component.Type, registered)) missing.Add(component.Type.FullName!);
        foreach (var tag in schema.Tags)
            if (tag is not null && NeedsRegistration(tag.Type, registered)) missing.Add(tag.Type.FullName!);

        Assert.That(missing, Is.Empty, "Missing registrations break a NativeAOT build. Check the RIN00024 warnings.");
    }

    [Test]
    public void TheGeneratorRegistersRinWorldAndTestComponents()
    {
        var registered = ComponentRegistry.GetRegisteredTypes();

        Assert.That(registered, Does.Contain(typeof(LocalTransform)));
        Assert.That(registered, Does.Contain(typeof(Health)));
        Assert.That(registered, Does.Contain(typeof(Stunned)));
    }

    [Test]
    public void RegisteringTwiceIsIgnored()
    {
        var before = ComponentRegistry.GetRegisteredTypes().Length;

        ComponentRegistry.RegisterComponent<Health>();

        Assert.That(ComponentRegistry.GetRegisteredTypes(), Has.Length.EqualTo(before));
    }

    private static bool NeedsRegistration(Type type, HashSet<Type> registered) =>
        type.Assembly != typeof(EntityStore).Assembly && !registered.Contains(type);
}
