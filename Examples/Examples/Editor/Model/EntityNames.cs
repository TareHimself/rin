using Rin.World;

namespace Examples.Editor.Model;

/// <summary>
///     The names the editor shows for entities, kept beside the world the way a scene file would, not as a component.
/// </summary>
/// <remarks>
///     An unnamed entity falls back to <see cref="HierarchyRows.LabelOf" />, its id and its components.
/// </remarks>
public sealed class EntityNames
{
    private readonly Dictionary<Entity, string> _names = [];

    public void Set(Entity entity, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) _names.Remove(entity);
        else _names[entity] = name.Trim();
    }

    public void SetAll(IReadOnlyDictionary<Entity, string> names)
    {
        foreach (var (entity, name) in names) Set(entity, name);
    }

    public void Clear() => _names.Clear();

    public string? NameOf(Entity entity) => _names.GetValueOrDefault(entity);

    public string LabelOf(World world, Entity entity) => NameOf(entity) ?? HierarchyRows.LabelOf(world, entity);
}
