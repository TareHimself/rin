using Rin.World;
using Rin.World.Editing;

namespace Examples.Editor.Model;

public enum FieldRowKind
{
    Number,
    Rotation,
    Bool,
    Enum,
    Entity,
    Group,
    ProcessLocal,
    Fixed
}

/// <summary>
///     One number of a <see cref="FieldRowKind.Number" /> row, at <paramref name="Offset" /> bytes into the component.
/// </summary>
public readonly record struct Lane(string Label, FieldKind Kind, int Offset);

/// <param name="Offset">Bytes from the start of the component, not of the struct that declares the field.</param>
public sealed record FieldRow(string Label, int Depth, FieldRowKind Kind, FieldType Type, int Offset, bool IsReadOnly,
    Lane[] Lanes);

/// <summary>
///     Flattens a component's fields into the rows an inspector draws, nested structs as a group row followed by their
///     fields one level deeper.
/// </summary>
/// <remarks>
///     Vectors, and structs of up to four numbers like a color, become one row with a lane per number.
/// </remarks>
public static class FieldRows
{
    private const int MaxLanes = 4;

    public static List<FieldRow> Of(ComponentInfo info)
    {
        var rows = new List<FieldRow>();
        AddFields(info.Fields, 0, 0, info.IsDerived, rows);
        return rows;
    }

    private static void AddFields(ComponentField[] fields, int baseOffset, int depth, bool readOnly, List<FieldRow> rows)
    {
        foreach (var field in fields)
        {
            var offset = baseOffset + field.Offset;
            var isReadOnly = readOnly || !ComponentJson.IsEditable(field);
            var kind = KindOf(field);
            rows.Add(new FieldRow(field.Name, depth, kind, field.Type, offset, isReadOnly,
                kind == FieldRowKind.Number ? LanesOf(field.Type, offset)! : []));
            if (kind == FieldRowKind.Group) AddFields(field.Type.Fields, offset, depth + 1, isReadOnly, rows);
        }
    }

    private static FieldRowKind KindOf(ComponentField field)
    {
        if (field.IsProcessLocal) return FieldRowKind.ProcessLocal;
        if (LanesOf(field.Type, 0) is not null) return FieldRowKind.Number;

        return field.Type.Kind switch
        {
            FieldKind.Quaternion => FieldRowKind.Rotation,
            FieldKind.Bool => FieldRowKind.Bool,
            FieldKind.Enum => FieldRowKind.Enum,
            FieldKind.Entity => FieldRowKind.Entity,
            FieldKind.Struct => FieldRowKind.Group,
            _ => FieldRowKind.Fixed
        };
    }

    private static Lane[]? LanesOf(FieldType type, int offset) => type.Kind switch
    {
        _ when FieldLanes.IsNumber(type.Kind) => [new Lane(string.Empty, type.Kind, offset)],
        FieldKind.Vector2 => Floats(offset, "X", "Y"),
        FieldKind.Vector3 => Floats(offset, "X", "Y", "Z"),
        FieldKind.Vector4 => Floats(offset, "X", "Y", "Z", "W"),
        FieldKind.Struct when IsSmallNumberStruct(type) => StructLanes(type, offset),
        _ => null
    };

    private static Lane[] Floats(int offset, params string[] labels)
    {
        var lanes = new Lane[labels.Length];
        for (var i = 0; i < lanes.Length; i++)
            lanes[i] = new Lane(labels[i], FieldKind.Float32, offset + i * sizeof(float));
        return lanes;
    }

    private static bool IsSmallNumberStruct(FieldType type)
    {
        if (type.Fields.Length is 0 or > MaxLanes) return false;

        foreach (var field in type.Fields)
            if (!FieldLanes.IsNumber(field.Type.Kind) || !ComponentJson.IsEditable(field))
                return false;
        return true;
    }

    private static Lane[] StructLanes(FieldType type, int offset)
    {
        var lanes = new Lane[type.Fields.Length];
        for (var i = 0; i < lanes.Length; i++)
        {
            var field = type.Fields[i];
            lanes[i] = new Lane(field.Name, field.Type.Kind, offset + field.Offset);
        }

        return lanes;
    }
}
