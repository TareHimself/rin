using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     Describes one component type in a <see cref="ComponentSchema" />. A size of zero makes it a tag, which has no
///     column.
/// </summary>
/// <remarks>
///     Every component is a C# struct, so infos come only from <see cref="Of{T}()" />.
/// </remarks>
public sealed record ComponentInfo
{
    private readonly ulong _layoutHash;
    private readonly byte[] _defaultValue;

    private ComponentInfo(string name, Type type, int typeIndex, int size, bool isSaved, bool isDerived,
        byte[] defaultValue, ComponentField[] fields, Type[] requires)
    {
        Name = name;
        Type = type;
        TypeIndex = typeIndex;
        Size = size;
        IsSaved = isSaved;
        IsDerived = isDerived;
        _defaultValue = defaultValue;
        Fields = fields;
        Requires = requires;
        _layoutHash = ComputeLayoutHash();
    }

    public string Name { get; }

    public Type Type { get; }

    /// <summary>
    ///     The struct's <see cref="Ecs.TypeIndex{T}" />, so a schema maps it to an id without a generic type argument.
    /// </summary>
    internal int TypeIndex { get; }

    public int Size { get; }

    /// <summary>
    ///     False for <see cref="EcsNotSavedAttribute" /> components, whose values are left out of every save.
    /// </summary>
    public bool IsSaved { get; }

    /// <summary>
    ///     True for <see cref="EcsDerivedAttribute" /> components, which are computed from other data.
    /// </summary>
    public bool IsDerived { get; }

    /// <summary>
    ///     The bytes of <c>new T()</c>, field initializers included, for adding the component without its C# type.
    /// </summary>
    public ReadOnlySpan<byte> DefaultValue => _defaultValue;

    /// <summary>
    ///     The instance fields, filled by a generated <see cref="ComponentModule" /> for saved components and empty
    ///     otherwise.
    /// </summary>
    public ComponentField[] Fields { get; }

    /// <summary>
    ///     The components this one needs, from its <see cref="EcsRequiresAttribute{T}" />s, as filled by a generated
    ///     <see cref="ComponentModule" />.
    /// </summary>
    public Type[] Requires { get; }

    public bool IsTag => Size == 0;

    /// <summary>
    ///     A tag for an <see cref="ITagData" />, otherwise a component of the struct's size.
    /// </summary>
    public static ComponentInfo Of<T>() where T : unmanaged => Of<T>([], []);

    /// <summary>
    ///     <see cref="Of{T}()" /> with the struct's fields and requirements, which a generated
    ///     <see cref="ComponentModule" /> passes.
    /// </summary>
    public static ComponentInfo Of<T>(ComponentField[] fields, Type[] requires) where T : unmanaged
    {
        var isTag = typeof(ITagData).IsAssignableFrom(typeof(T));
        if (isTag && typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0)
            throw new ArgumentException($"{typeof(T).Name} is an {nameof(ITagData)}, so it cannot have fields.");
        var defaultValue = new T();
        return new ComponentInfo(
            typeof(T).Name,
            typeof(T),
            Ecs.TypeIndex<T>.Value,
            isTag ? 0 : Unsafe.SizeOf<T>(),
            !typeof(T).IsDefined(typeof(EcsNotSavedAttribute), false),
            typeof(T).IsDefined(typeof(EcsDerivedAttribute), false),
            isTag ? [] : MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in defaultValue)).ToArray(),
            fields,
            requires);
    }

    public bool TryGetField(string name, out ComponentField field) => ComponentField.TryFind(Fields, name, out field);

    /// <summary>
    ///     A hash of the name, size, <see cref="IsSaved" /> and fields, the same in every process, so a snapshot can check
    ///     it restores into a schema that lays its components out the same way.
    /// </summary>
    public ulong LayoutHash() => _layoutHash;

    private ulong ComputeLayoutHash()
    {
        var hash = Fnv.Mix(Fnv.Basis, Name);
        hash = Fnv.Mix(hash, (uint)Size);
        hash = Fnv.Mix(hash, IsSaved ? 1u : 0u);
        foreach (var field in Fields) hash = field.Mix(hash);
        return hash;
    }
}

/// <summary>
///     One field of a component or of a <see cref="FieldKind.Struct" />, for code that reads and writes it by name.
/// </summary>
/// <param name="Offset">Bytes from the start of the struct that declares the field.</param>
public readonly record struct ComponentField(string Name, FieldType Type, int Offset, bool IsProcessLocal, bool IsDerived)
{
    /// <summary>
    ///     This field's bytes within <paramref name="owner" />, the bytes of the struct that declares it.
    /// </summary>
    public Span<byte> Slice(Span<byte> owner) => owner.Slice(Offset, Type.Size);

    public ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> owner) => owner.Slice(Offset, Type.Size);

    public T Read<T>(ReadOnlySpan<byte> owner) where T : unmanaged
    {
        EnsureIs<T>();
        return MemoryMarshal.Read<T>(Slice(owner));
    }

    public void Write<T>(Span<byte> owner, in T value) where T : unmanaged
    {
        EnsureIs<T>();
        MemoryMarshal.Write(Slice(owner), in value);
    }

    internal static bool TryFind(ComponentField[] fields, string name, out ComponentField field)
    {
        foreach (var candidate in fields)
        {
            if (candidate.Name != name) continue;
            field = candidate;
            return true;
        }

        field = default;
        return false;
    }

    private void EnsureIs<T>()
    {
        if (typeof(T) != Type.Type)
            throw new ArgumentException($"Field {Name} is a {Type.Type.Name}, not a {typeof(T).Name}.");
    }

    internal ulong Mix(ulong hash)
    {
        hash = Fnv.Mix(Fnv.Mix(hash, Name), (ulong)Offset);
        return Type.Mix(Fnv.Mix(hash, IsProcessLocal ? 1u : 0u));
    }
}
