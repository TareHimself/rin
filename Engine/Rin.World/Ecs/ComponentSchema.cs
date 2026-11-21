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
public sealed partial class ComponentSchema
{
    private const int Absent = -1;

    private readonly Lock _gate = new();
    private readonly Dictionary<Type, ComponentId> _byType;
    private ComponentInfo[] _infos;
    private int[] _localIds;
    private int _count;

    public ComponentSchema()
    {
        _byType = [];
        _infos = new ComponentInfo[16];
        _localIds = [];
    }

    private ComponentSchema(ComponentSchema source)
    {
        lock (source._gate)
        {
            _byType = new Dictionary<Type, ComponentId>(source._byType);
            _infos = (ComponentInfo[])source._infos.Clone();
            _localIds = (int[])source._localIds.Clone();
            _count = source._count;
        }
    }

    /// <summary>
    ///     Every component and tag of Rin.World, frozen, so clones of it never collide with a later append.
    /// </summary>
    public static ComponentSchema Engine { get; } = CreateEngine();

    public int Count => Volatile.Read(ref _count);

    public bool IsFrozen { get; private set; }

    public ComponentInfo this[ComponentId id] =>
        (uint)id.Value < (uint)Count ? _infos[id.Value] : throw new ArgumentOutOfRangeException(nameof(id), id, null);

    public ComponentSchema Clone() => new(this);

    /// <summary>
    ///     Stops any further type from being added, lazily or through <see cref="Add" />.
    /// </summary>
    public void Freeze()
    {
        lock (_gate) IsFrozen = true;
    }

    public ComponentId Add(ComponentInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        lock (_gate)
        {
            EnsureNotFrozen(info.Name);
            if (info.Type is not null && _byType.ContainsKey(info.Type))
                throw new ArgumentException($"{info.Name} is already in the schema.", nameof(info));
            return Append(info);
        }
    }

    /// <summary>
    ///     The id of <typeparamref name="T" />, added on first use unless the schema is frozen.
    /// </summary>
    public ComponentId IdOf<T>() where T : unmanaged =>
        TryGetId<T>(out var id) ? id : AddLazily<T>();

    public bool TryGetId<T>(out ComponentId id) where T : unmanaged
    {
        var index = TypeIndex<T>.Value;
        var localIds = Volatile.Read(ref _localIds);
        var value = index < localIds.Length ? localIds[index] : Absent;
        id = new ComponentId(value);
        return value != Absent;
    }

    private ComponentId AddLazily<T>() where T : unmanaged
    {
        lock (_gate)
        {
            if (TryGetId<T>(out var existing)) return existing;
            if (!_byType.TryGetValue(typeof(T), out var id))
            {
                EnsureNotFrozen(typeof(T).Name);
                id = Append(ComponentInfo.Of<T>());
            }

            MapTypeIndex(TypeIndex<T>.Value, id);
            return id;
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
        if (info.Type is not null) _byType.Add(info.Type, id);
        Volatile.Write(ref _count, _count + 1);
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

    private void EnsureNotFrozen(string name)
    {
        if (IsFrozen) throw new InvalidOperationException($"{name} cannot be added because the schema is frozen.");
    }
}
