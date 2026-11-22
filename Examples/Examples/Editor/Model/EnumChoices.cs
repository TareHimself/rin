using System.Runtime.InteropServices;
using Rin.World;

namespace Examples.Editor.Model;

/// <summary>
///     The named values of an <see cref="FieldKind.Enum" /> field, compared as raw bits so any underlying type works.
/// </summary>
public sealed class EnumChoices
{
    private readonly FieldType _type;
    private readonly string[] _names;
    private readonly ulong[] _values;

    public EnumChoices(FieldType type)
    {
        _type = type;
        _names = Enum.GetNames(type.Type);
        var values = Enum.GetValuesAsUnderlyingType(type.Type);
        _values = new ulong[values.Length];
        for (var i = 0; i < _values.Length; i++) _values[i] = RawOf(values.GetValue(i)!);
    }

    public ulong Read(ReadOnlySpan<byte> bytes)
    {
        Span<byte> wide = stackalloc byte[sizeof(ulong)];
        wide.Clear();
        bytes[.._type.Size].CopyTo(wide);
        return MemoryMarshal.Read<ulong>(wide);
    }

    public void Write(Span<byte> bytes, ulong value)
    {
        Span<byte> wide = stackalloc byte[sizeof(ulong)];
        MemoryMarshal.Write(wide, in value);
        wide[.._type.Size].CopyTo(bytes);
    }

    public string NameOf(ulong value)
    {
        var index = Array.IndexOf(_values, value);
        return index >= 0 ? _names[index] : value.ToString();
    }

    /// <summary>
    ///     Every named value, in the order <see cref="Enum.GetValues(Type)" /> gives: ascending as unsigned numbers.
    /// </summary>
    public IReadOnlyList<ulong> Values => _values;

    private static ulong RawOf(object value) => value switch
    {
        sbyte v => (byte)v,
        byte v => v,
        short v => (ushort)v,
        ushort v => v,
        int v => (uint)v,
        uint v => v,
        long v => (ulong)v,
        ulong v => v,
        _ => throw new ArgumentException($"{value.GetType()} is not an enum's underlying type.", nameof(value))
    };
}
