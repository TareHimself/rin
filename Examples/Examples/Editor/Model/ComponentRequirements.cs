using Rin.World;

namespace Examples.Editor.Model;

/// <summary>
///     What adding a component to an entity brings with it, and which of its components keep one from being removed.
/// </summary>
public static class ComponentRequirements
{
    /// <summary>
    ///     Replaces <paramref name="missing" /> with the requirements of <paramref name="id" /> the entity lacks, in the
    ///     order to add them.
    /// </summary>
    public static void CollectMissing(World world, Entity entity, ComponentId id, List<ComponentId> missing)
    {
        world.Schema.CollectRequirements(id, missing);
        missing.RemoveAll(required => world.Has(entity, required));
    }

    /// <summary>
    ///     Replaces <paramref name="dependents" /> with the entity's components that require <paramref name="id" />.
    /// </summary>
    public static void CollectDependents(World world, Entity entity, ComponentId id, List<ComponentId> dependents)
    {
        dependents.Clear();
        var type = world.Schema[id].Type;
        foreach (var other in world.ComponentsOf(entity))
            if (world.Schema[other].Requires.Contains(type))
                dependents.Add(other);
    }

    /// <summary>
    ///     Adds <paramref name="id" /> with its default value, after every requirement the entity lacks.
    /// </summary>
    public static void AddWithRequirements(World world, Entity entity, ComponentId id, List<ComponentId> scratch)
    {
        CollectMissing(world, entity, id, scratch);
        foreach (var required in scratch) world.AddDefault(entity, required);
        world.AddDefault(entity, id);
    }
}
