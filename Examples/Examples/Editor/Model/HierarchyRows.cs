using Rin.World;
using Rin.World.Transforms;

namespace Examples.Editor.Model;

public readonly record struct HierarchyRow(Entity Entity, int Depth, string Label, bool HasChildren, bool IsExpanded);

/// <summary>
///     Every live entity as a depth-first tree of rows, roots in id order, each labelled by its
///     <see cref="EntityNames.LabelOf" />.
/// </summary>
/// <remarks>
///     A filter keeps the entities whose label contains it and their ancestors, and shows every match even under a
///     collapsed parent.
/// </remarks>
public static class HierarchyRows
{
    private const int MaxNamedComponents = 2;

    public static void Build(World world, EntityNames names, List<Entity> scratch, IReadOnlySet<Entity> collapsed,
        string filter, List<HierarchyRow> rows)
    {
        rows.Clear();
        world.CollectEntities(scratch);
        foreach (var entity in scratch)
            if (!world.GetParent(entity).IsValid)
                AddTree(world, names, entity, 0, collapsed, filter, rows);
    }

    /// <summary>
    ///     The label for an unnamed entity: its id and the saved components that say what it is, skipping transforms.
    /// </summary>
    public static string LabelOf(World world, Entity entity)
    {
        var names = new List<string>();
        var others = 0;
        foreach (var id in world.ComponentsOf(entity))
        {
            var info = world.Schema[id];
            if (!info.IsSaved || info.IsDerived || info.Type == typeof(LocalTransform)) continue;
            if (names.Count < MaxNamedComponents) names.Add(info.Type.Name);
            else others++;
        }

        var label = names.Count == 0 ? "Entity" : string.Join(", ", names);
        return others == 0 ? $"#{entity.Id} {label}" : $"#{entity.Id} {label} +{others}";
    }

    // Returns whether the entity or one of its descendants made it into the rows.
    private static bool AddTree(World world, EntityNames names, Entity entity, int depth, IReadOnlySet<Entity> collapsed,
        string filter, List<HierarchyRow> rows)
    {
        var label = names.LabelOf(world, entity);
        var children = world.GetChildren(entity);
        var isFiltering = filter.Length > 0;
        var isExpanded = isFiltering || !collapsed.Contains(entity);
        var index = rows.Count;
        rows.Add(new HierarchyRow(entity, depth, label, children.Count > 0, isExpanded));

        var anyChildKept = false;
        if (isExpanded)
            foreach (var child in children)
                anyChildKept |= AddTree(world, names, child, depth + 1, collapsed, filter, rows);

        if (!isFiltering || anyChildKept || label.Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;

        rows.RemoveAt(index);
        return false;
    }
}
