using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     The component types a <see cref="World" /> knows, each with its <see cref="ComponentId" />. Worlds may share one
///     schema, so they share ids and can copy component bytes between each other.
/// </summary>
/// <remarks>
///     Types are only ever appended, under a lock, so an id never changes while a world uses it. Removing a type or
///     changing its layout means a new schema. <see cref="Clone" /> copies every id, which keeps the
///     <see cref="Engine" /> ids the same in every schema made from it.
/// </remarks>
public sealed class ComponentSchema
{
    private const int Absent = -1;

    private readonly Lock _gate = new();
    private ComponentInfo[] _infos;
    private int[] _localIds;
    private int _count;

    public ComponentSchema()
    {
        _infos = new ComponentInfo[16];
        _localIds = [];
    }

    private ComponentSchema(ComponentSchema source)
    {
        lock (source._gate)
        {
            _infos = (ComponentInfo[])source._infos.Clone();
            _localIds = (int[])source._localIds.Clone();
            _count = source._count;
        }
    }

    /// <summary>
    ///     <see cref="ComponentModule.Engine" />, frozen, so clones of it never collide with a later append.
    /// </summary>
    public static ComponentSchema Engine { get; } = Compose(ComponentModule.Engine);

    public int Count => Volatile.Read(ref _count);

    public bool IsFrozen { get; private set; }

    public ComponentInfo this[ComponentId id] =>
        (uint)id.Value < (uint)Count ? _infos[id.Value] : throw new ArgumentOutOfRangeException(nameof(id), id, null);

    /// <summary>
    ///     A frozen schema holding <paramref name="modules" /> in order, so the same modules in the same order always give
    ///     the same ids. A module or type already placed is skipped.
    /// </summary>
    public static ComponentSchema Compose(params ReadOnlySpan<ComponentModule> modules)
    {
        var schema = new ComponentSchema();
        foreach (var module in modules) schema.AppendModule(module);
        schema.Freeze();
        return schema;
    }

    /// <summary>
    ///     An unfrozen copy with the same ids, for worlds that add types on first use.
    /// </summary>
    public ComponentSchema Clone() => new(this);

    /// <summary>
    ///     Stops any further type from being added by <see cref="IdOf{T}" />.
    /// </summary>
    public void Freeze()
    {
        lock (_gate) IsFrozen = true;
    }

    /// <summary>
    ///     A hash of every component's <see cref="ComponentInfo.LayoutHash" /> in id order, so two processes can check
    ///     they agree on every id.
    /// </summary>
    public ulong Fingerprint()
    {
        var count = Count;
        var infos = Volatile.Read(ref _infos);
        var hash = Fnv.Basis;
        for (var i = 0; i < count; i++) hash = Fnv.Mix(hash, infos[i].LayoutHash());
        return hash;
    }

    /// <summary>
    ///     The id of <typeparamref name="T" />, added on first use unless the schema is frozen.
    /// </summary>
    public ComponentId IdOf<T>() where T : unmanaged =>
        TryGetId<T>(out var id) ? id : AddLazily<T>();

    public bool TryGetId<T>(out ComponentId id) where T : unmanaged
    {
        id = new ComponentId(LocalIdOf(TypeIndex<T>.Value));
        return id.Value != Absent;
    }

    private void AppendModule(ComponentModule module)
    {
        foreach (var info in module.Components)
            if (LocalIdOf(info.TypeIndex) == Absent) Append(info);
    }

    private int LocalIdOf(int typeIndex)
    {
        var localIds = Volatile.Read(ref _localIds);
        return typeIndex < localIds.Length ? localIds[typeIndex] : Absent;
    }

    private ComponentId AddLazily<T>() where T : unmanaged
    {
        lock (_gate)
        {
            if (TryGetId<T>(out var existing)) return existing;
            EnsureNotFrozen(typeof(T));
            return Append(ComponentInfo.Of<T>());
        }
    }

    private ComponentId Append(ComponentInfo info)
    {
        var id = new ComponentId(_count);
        if (_count == _infos.Length)
        {
            var grown = new ComponentInfo[_infos.Length * 2];
            _infos.CopyTo(grown, 0);
            Volatile.Write(ref _infos, grown);
        }

        _infos[id.Value] = info;
        Volatile.Write(ref _count, _count + 1);
        MapTypeIndex(info.TypeIndex, id);
        return id;
    }

    private void MapTypeIndex(int typeIndex, ComponentId id)
    {
        if (typeIndex >= _localIds.Length)
        {
            var grown = new int[int.Max(typeIndex + 1, _localIds.Length * 2)];
            grown.AsSpan().Fill(Absent);
            _localIds.CopyTo(grown, 0);
            grown[typeIndex] = id.Value;
            Volatile.Write(ref _localIds, grown);
            return;
        }

        Volatile.Write(ref _localIds[typeIndex], id.Value);
    }

    private void EnsureNotFrozen(Type type)
    {
        if (IsFrozen)
            throw new InvalidOperationException(
                $"{type.FullName} is not in this frozen schema. Compose it with the component module of {type.Assembly.GetName().Name}.");
    }
}
