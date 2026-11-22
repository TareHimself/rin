using System.Runtime.CompilerServices;
using Rin.World.Ecs;

namespace Rin.World;

/// <summary>
///     The type of a <see cref="ComponentField" />, shared by every field of that type in a generated
///     <see cref="ComponentModule" />.
/// </summary>
public sealed class FieldType
{
    private FieldType(Type type, FieldKind kind, int size, ComponentField[] fields)
    {
        Type = type;
        Kind = kind;
        Size = size;
        Fields = fields;
    }

    public Type Type { get; }

    public FieldKind Kind { get; }

    public int Size { get; }

    /// <summary>
    ///     The fields of a <see cref="FieldKind.Struct" />, at offsets from its start. Empty for every other kind.
    /// </summary>
    public ComponentField[] Fields { get; }

    public bool TryGetField(string name, out ComponentField field) => ComponentField.TryFind(Fields, name, out field);

    public static FieldType Of<T>(FieldKind kind) where T : unmanaged =>
        kind == FieldKind.Struct
            ? throw new ArgumentException($"A {nameof(FieldKind.Struct)} needs its fields, so use {nameof(Struct)}.", nameof(kind))
            : new FieldType(typeof(T), kind, Unsafe.SizeOf<T>(), []);

    public static FieldType Struct<T>(ComponentField[] fields) where T : unmanaged =>
        new(typeof(T), FieldKind.Struct, Unsafe.SizeOf<T>(), fields);

    internal ulong Mix(ulong hash)
    {
        hash = Fnv.Mix(Fnv.Mix(hash, (ulong)Kind), (ulong)Size);
        foreach (var field in Fields) hash = field.Mix(hash);
        return hash;
    }
}
