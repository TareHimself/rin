using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;

namespace Rin.World.Ecs;

/// <summary>
///     Copies one component type's archetype column to and from a <see cref="WorldSnapshot" /> as raw bytes, so the
///     snapshot walk needs no generic code per component.
/// </summary>
internal abstract class ComponentColumn
{
    /// <summary>
    ///     False for <see cref="IDerivedComponentData" /> and the backend's hierarchy component, whose column is cleared on
    ///     restore and rebuilt instead.
    /// </summary>
    public abstract bool IsSaved { get; }

    public abstract void Save(Archetype archetype, WorldSnapshot snapshot);

    public abstract void Restore(Archetype archetype, int start, int count, ref SnapshotReader reader);

    public abstract void Clear(Archetype archetype, int start, int count);
}

internal sealed class ComponentColumn<T> : ComponentColumn where T : struct, IComponent
{
    private static readonly bool Derived = typeof(IDerivedComponentData).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(TreeNode);

    public override bool IsSaved => !Derived;

    public override void Save(Archetype archetype, WorldSnapshot snapshot)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new InvalidOperationException(
                $"{typeof(T).Name} holds managed references and cannot be saved in a snapshot. Keep managed data in a registry and store its id.");
        snapshot.WriteBytes(MemoryMarshal.AsBytes(Column(archetype, 0, archetype.Count)));
    }

    public override void Restore(Archetype archetype, int start, int count, ref SnapshotReader reader) =>
        reader.ReadBytes(count * Unsafe.SizeOf<T>()).CopyTo(MemoryMarshal.AsBytes(Column(archetype, start, count)));

    public override void Clear(Archetype archetype, int start, int count) => Column(archetype, start, count).Clear();

    private static Span<T> Column(Archetype archetype, int start, int count) => archetype.Components<T>().Slice(start, count);
}

/// <summary>
///     The column of every component type that was ever added, indexed by the backend's struct index.
/// </summary>
internal static class ComponentColumns
{
    private static readonly ComponentColumn?[] Columns = CreateColumns();

    public static void Register<T>() where T : struct, IComponent
    {
        ref var slot = ref Columns[ComponentIndex<T>.Value];
        slot ??= new ComponentColumn<T>();
    }

    public static ComponentColumn Get(ComponentType type) =>
        Columns[type.StructIndex] ?? throw new InvalidOperationException($"{type.Type.Name} was never added through the Rin.World API.");

    private static ComponentColumn?[] CreateColumns()
    {
        var columns = new ComponentColumn?[EntityStore.GetEntitySchema().Components.Length];
        columns[ComponentIndex<TreeNode>.Value] = new ComponentColumn<TreeNode>();
        return columns;
    }
}

internal static class ComponentIndex<T> where T : struct, IComponent
{
    public static readonly int Value = EntityStore.GetEntitySchema().GetComponentType<T>().StructIndex;
}
