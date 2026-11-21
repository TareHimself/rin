using System.Reflection;
using System.Runtime.CompilerServices;
using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     Describes one component type in a <see cref="ComponentSchema" />. A size of zero makes it a tag, which has no
///     column.
/// </summary>
/// <remarks>
///     Every component is a C# struct, so infos come only from <see cref="Of{T}" />.
/// </remarks>
public sealed record ComponentInfo
{
    private ComponentInfo(string name, Type type, int size, bool isSaved)
    {
        Name = name;
        Type = type;
        Size = size;
        IsSaved = isSaved;
    }

    public string Name { get; }

    public Type Type { get; }

    public int Size { get; }

    /// <summary>
    ///     False for <see cref="EcsNotSavedAttribute" /> components, whose values are left out of every save.
    /// </summary>
    public bool IsSaved { get; }

    /// <summary>
    ///     Empty until field offsets are generated for the inspector.
    /// </summary>
    public ComponentField[] Fields { get; } = [];

    public bool IsTag => Size == 0;

    /// <summary>
    ///     A tag for an <see cref="ITagData" />, otherwise a component of the struct's size.
    /// </summary>
    public static ComponentInfo Of<T>() where T : unmanaged
    {
        var isTag = typeof(ITagData).IsAssignableFrom(typeof(T));
        if (isTag && typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0)
            throw new ArgumentException($"{typeof(T).Name} is an {nameof(ITagData)}, so it cannot have fields.");
        return new ComponentInfo(
            typeof(T).Name,
            typeof(T),
            isTag ? 0 : Unsafe.SizeOf<T>(),
            !typeof(T).IsDefined(typeof(EcsNotSavedAttribute), false));
    }

    /// <summary>
    ///     A hash of the name, size and <see cref="IsSaved" />, the same in every process, so a snapshot can check it
    ///     restores into a schema that lays its components out the same way.
    /// </summary>
    internal ulong LayoutHash()
    {
        const ulong prime = 1099511628211;
        var hash = 14695981039346656037;
        foreach (var c in Name) hash = (hash ^ c) * prime;
        hash = (hash ^ (uint)Size) * prime;
        return (hash ^ (IsSaved ? 1u : 0u)) * prime;
    }
}

/// <summary>
///     One field of a component, for code that reads and writes components by name.
/// </summary>
public readonly record struct ComponentField(string Name, Type Type, int Offset);
