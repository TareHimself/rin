using System.Runtime.CompilerServices;
using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     Describes one component type in a <see cref="ComponentSchema" />. A size of zero makes it a tag, which has no
///     column.
/// </summary>
/// <param name="Type">
///     The C# struct, or null for a component defined only at run time.
/// </param>
/// <param name="IsDerived">
///     Its bytes are left out of snapshots, as for <see cref="IDerivedComponentData" />.
/// </param>
/// <param name="Fields">
///     Filled for run-time components. Empty for C# structs for now.
/// </param>
public sealed record ComponentInfo(
    string Name,
    Type? Type,
    int Size,
    int Alignment,
    bool IsDerived,
    bool IsEditorOnly,
    ComponentField[] Fields)
{
    public bool IsTag => Size == 0;

    /// <summary>
    ///     Such a component lives in a typed column instead of raw bytes and cannot be saved in a snapshot.
    /// </summary>
    public bool ContainsReferences { get; private init; }

    internal Func<Column>? CreateManagedColumn { get; private init; }

    /// <summary>
    ///     A tag for an <see cref="ITagData" />, otherwise a component of the struct's size.
    /// </summary>
    public static ComponentInfo Of<T>() where T : struct
    {
        var isTag = typeof(ITagData).IsAssignableFrom(typeof(T));
        var containsReferences = !isTag && RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        return new ComponentInfo(
            typeof(T).Name,
            typeof(T),
            isTag ? 0 : Unsafe.SizeOf<T>(),
            isTag ? 1 : Unsafe.SizeOf<AlignmentProbe<T>>() - Unsafe.SizeOf<T>(),
            typeof(IDerivedComponentData).IsAssignableFrom(typeof(T)),
            false,
            [])
        {
            ContainsReferences = containsReferences,
            CreateManagedColumn = containsReferences ? static () => new ManagedColumn<T>() : null
        };
    }

    // Its size is the struct's size plus the padding a byte in front of it needs, which is its alignment.
    private struct AlignmentProbe<T> where T : struct
    {
#pragma warning disable CS0169
        private byte _pad;
        private T _value;
#pragma warning restore CS0169
    }
}

/// <summary>
///     One field of a component, for code that reads and writes components by name.
/// </summary>
public readonly record struct ComponentField(string Name, Type Type, int Offset);
