using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rin.World.Editing;

/// <summary>
///     Component schemas and field values as JSON, for level editors. A value is a JSON number, bool, array or object by
///     its <see cref="FieldKind" />, and a struct of numbers is an array of its fields in order.
/// </summary>
/// <remarks>
///     Editors see only editable data: saved components that are not derived, and of those, fields that are neither
///     process-local nor derived. A component with no editable field is left out unless it is a tag.
/// </remarks>
public static class ComponentJson
{
    /// <summary>
    ///     Writes the schema of the engine's components and every component module in <paramref name="assemblies" />, and
    ///     returns how many components it lists.
    /// </summary>
    public static int WriteSchema(IEnumerable<Assembly> assemblies, Stream stream)
    {
        List<ComponentModule> modules = [ComponentModule.Engine];
        foreach (var assembly in assemblies)
            if (ComponentModule.Of(assembly) is { } module)
                modules.Add(module);
        return WriteSchema(ComponentSchema.Compose([.. modules]), stream);
    }

    public static int WriteSchema(ComponentSchema schema, Stream stream)
    {
        var editable = new List<ComponentInfo>();
        for (var i = 0; i < schema.Count; i++)
            if (IsEditable(schema[new ComponentId(i)]))
                editable.Add(schema[new ComponentId(i)]);
        editable.Sort(static (a, b) => string.CompareOrdinal(a.Type.FullName, b.Type.FullName));

        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteStartArray("components");
        foreach (var info in editable) WriteComponent(writer, info);
        writer.WriteEndArray();
        writer.WriteEndObject();
        return editable.Count;
    }

    public static bool IsEditable(ComponentInfo info)
    {
        if (!info.IsSaved || info.IsDerived) return false;
        if (info.IsTag) return true;
        foreach (var field in info.Fields)
            if (IsEditable(field))
                return true;
        return false;
    }

    public static bool IsEditable(ComponentField field) => !field.IsProcessLocal && !field.IsDerived;

    /// <summary>
    ///     Writes each editable field named in <paramref name="values" /> into <paramref name="component" />, the
    ///     component's bytes, and leaves the rest as they are. Keys that name no editable field are ignored. A value of the
    ///     wrong shape throws <see cref="InvalidDataException" />.
    /// </summary>
    /// <param name="findEntity">Resolves the object name an <see cref="FieldKind.Entity" /> field holds.</param>
    public static void ReadFields(ComponentInfo info, JsonObject values, Span<byte> component, Func<string, Entity?> findEntity) =>
        ReadStruct(info.Fields, values, component, findEntity);

    /// <summary>
    ///     <see cref="ReadFields" /> for one <see cref="FieldKind.Struct" /> value, such as a collider shape on its own.
    /// </summary>
    public static void ReadStruct(FieldType type, JsonNode value, Span<byte> bytes, Func<string, Entity?> findEntity) =>
        ReadStructValue(type, value, bytes, findEntity);

    private static void WriteComponent(Utf8JsonWriter writer, ComponentInfo info)
    {
        writer.WriteStartObject();
        writer.WriteString("name", info.Type.FullName);
        writer.WriteString("displayName", info.Name);
        WriteFieldList(writer, info.Fields);
        writer.WritePropertyName("defaults");
        WriteStructValue(writer, info.Fields, info.DefaultValue, asArray: false);
        writer.WriteEndObject();
    }

    private static void WriteFieldList(Utf8JsonWriter writer, ComponentField[] fields)
    {
        writer.WriteStartArray("fields");
        foreach (var field in fields)
            if (IsEditable(field))
                WriteField(writer, field);
        writer.WriteEndArray();
    }

    private static void WriteField(Utf8JsonWriter writer, ComponentField field)
    {
        writer.WriteStartObject();
        writer.WriteString("name", field.Name);
        writer.WriteString("type", TypeNameOf(field.Type.Kind));
        writer.WriteString("typeName", field.Type.Type.FullName);
        if (field.Type.Kind == FieldKind.Enum)
        {
            writer.WriteStartArray("enumNames");
            foreach (var name in Enum.GetNames(field.Type.Type)) writer.WriteStringValue(name);
            writer.WriteEndArray();
        }

        if (field.Type.Kind == FieldKind.Struct) WriteFieldList(writer, field.Type.Fields);
        writer.WriteEndObject();
    }

    private static string TypeNameOf(FieldKind kind) => kind switch
    {
        FieldKind.Float32 or FieldKind.Float64 => "float",
        FieldKind.Int8 or FieldKind.UInt8 or FieldKind.Int16 or FieldKind.UInt16 or FieldKind.Int32 or FieldKind.UInt32
            or FieldKind.Int64 or FieldKind.UInt64 => "int",
        _ => JsonNamingPolicy.CamelCase.ConvertName(kind.ToString())
    };

    private static void WriteValue(Utf8JsonWriter writer, ComponentField field, ReadOnlySpan<byte> owner)
    {
        var bytes = field.Slice(owner);
        switch (field.Type.Kind)
        {
            case FieldKind.Bool: writer.WriteBooleanValue(bytes[0] != 0); break;
            case FieldKind.Float32: writer.WriteNumberValue(MemoryMarshal.Read<float>(bytes)); break;
            case FieldKind.Float64: writer.WriteNumberValue(MemoryMarshal.Read<double>(bytes)); break;
            case FieldKind.UInt64: writer.WriteNumberValue(MemoryMarshal.Read<ulong>(bytes)); break;
            case FieldKind.Int8 or FieldKind.UInt8 or FieldKind.Int16 or FieldKind.UInt16 or FieldKind.Int32
                or FieldKind.UInt32 or FieldKind.Int64:
                writer.WriteNumberValue(ReadInteger(field.Type.Kind, bytes));
                break;
            case FieldKind.Vector2 or FieldKind.Vector3 or FieldKind.Vector4 or FieldKind.Quaternion or FieldKind.Matrix4x4:
                writer.WriteStartArray();
                foreach (var value in MemoryMarshal.Cast<byte, float>(bytes)) writer.WriteNumberValue(value);
                writer.WriteEndArray();
                break;
            case FieldKind.Enum: writer.WriteStringValue(EnumNameOf(field.Type, bytes)); break;
            case FieldKind.Struct: WriteStructValue(writer, field.Type.Fields, bytes, IsNumeric(field.Type)); break;
            default: writer.WriteNullValue(); break;
        }
    }

    private static void WriteStructValue(Utf8JsonWriter writer, ComponentField[] fields, ReadOnlySpan<byte> bytes, bool asArray)
    {
        if (asArray) writer.WriteStartArray();
        else writer.WriteStartObject();
        foreach (var field in fields)
        {
            if (!IsEditable(field)) continue;
            if (!asArray) writer.WritePropertyName(field.Name);
            WriteValue(writer, field, bytes);
        }

        if (asArray) writer.WriteEndArray();
        else writer.WriteEndObject();
    }

    private static void ReadStruct(ComponentField[] fields, JsonObject values, Span<byte> owner, Func<string, Entity?> findEntity)
    {
        foreach (var field in fields)
            if (IsEditable(field) && values[field.Name] is { } value)
                ReadValue(field, value, owner, findEntity);
    }

    private static void ReadValue(ComponentField field, JsonNode value, Span<byte> owner, Func<string, Entity?> findEntity)
    {
        var bytes = field.Slice(owner);
        switch (field.Type.Kind)
        {
            case FieldKind.Bool: bytes[0] = ReadBool(value) ? (byte)1 : (byte)0; break;
            case FieldKind.Float32: MemoryMarshal.Write(bytes, (float)ReadNumber(value)); break;
            case FieldKind.Float64: MemoryMarshal.Write(bytes, ReadNumber(value)); break;
            case FieldKind.Int8 or FieldKind.UInt8 or FieldKind.Int16 or FieldKind.UInt16 or FieldKind.Int32
                or FieldKind.UInt32 or FieldKind.Int64 or FieldKind.UInt64:
                WriteInteger(field.Type.Kind, bytes, (long)ReadNumber(value));
                break;
            case FieldKind.Vector2 or FieldKind.Vector3 or FieldKind.Vector4 or FieldKind.Quaternion or FieldKind.Matrix4x4:
                ReadNumbers(value, MemoryMarshal.Cast<byte, float>(bytes), bytes.Length / sizeof(float));
                break;
            case FieldKind.Enum: WriteInteger(KindOfSize(field.Type.Size), bytes, ReadEnum(field.Type, value)); break;
            case FieldKind.Entity: MemoryMarshal.Write(bytes, ReadEntity(value, findEntity)); break;
            case FieldKind.Struct: ReadStructValue(field.Type, value, bytes, findEntity); break;
        }
    }

    private static void ReadStructValue(FieldType type, JsonNode value, Span<byte> bytes, Func<string, Entity?> findEntity)
    {
        if (value is JsonObject values)
        {
            ReadStruct(type.Fields, values, bytes, findEntity);
            return;
        }

        if (value is not JsonArray array || !IsNumeric(type) || array.Count > type.Fields.Length)
            throw new InvalidDataException($"Expected a {type.Type.Name} object, got {value.ToJsonString()}.");
        // A shorter array leaves the trailing fields as they are, so a color without alpha keeps its default alpha.
        for (var i = 0; i < array.Count; i++)
            ReadValue(type.Fields[i], array[i] ?? throw new InvalidDataException("A number is missing."), bytes, findEntity);
    }

    private static bool IsNumeric(FieldType type)
    {
        if (type.Fields.Length == 0) return false;
        foreach (var field in type.Fields)
            if (field.Type.Kind is not (FieldKind.Float32 or FieldKind.Float64 or FieldKind.Int8 or FieldKind.UInt8
                or FieldKind.Int16 or FieldKind.UInt16 or FieldKind.Int32 or FieldKind.UInt32 or FieldKind.Int64
                or FieldKind.UInt64) || !IsEditable(field))
                return false;
        return true;
    }

    private static long ReadInteger(FieldKind kind, ReadOnlySpan<byte> bytes) => kind switch
    {
        FieldKind.Int8 => (sbyte)bytes[0],
        FieldKind.UInt8 => bytes[0],
        FieldKind.Int16 => MemoryMarshal.Read<short>(bytes),
        FieldKind.UInt16 => MemoryMarshal.Read<ushort>(bytes),
        FieldKind.Int32 => MemoryMarshal.Read<int>(bytes),
        FieldKind.UInt32 => MemoryMarshal.Read<uint>(bytes),
        _ => MemoryMarshal.Read<long>(bytes)
    };

    private static void WriteInteger(FieldKind kind, Span<byte> bytes, long value)
    {
        switch (kind)
        {
            case FieldKind.Int8 or FieldKind.UInt8: bytes[0] = (byte)value; break;
            case FieldKind.Int16 or FieldKind.UInt16: MemoryMarshal.Write(bytes, (short)value); break;
            case FieldKind.Int32 or FieldKind.UInt32: MemoryMarshal.Write(bytes, (int)value); break;
            default: MemoryMarshal.Write(bytes, value); break;
        }
    }

    private static FieldKind KindOfSize(int size) => size switch
    {
        1 => FieldKind.UInt8,
        2 => FieldKind.UInt16,
        4 => FieldKind.UInt32,
        _ => FieldKind.UInt64
    };

    private static string EnumNameOf(FieldType type, ReadOnlySpan<byte> bytes)
    {
        var value = ReadInteger(KindOfSize(type.Size), bytes);
        var names = Enum.GetNames(type.Type);
        var values = Enum.GetValuesAsUnderlyingType(type.Type);
        for (var i = 0; i < names.Length; i++)
            if (Convert.ToInt64(values.GetValue(i), CultureInfo.InvariantCulture) == value)
                return names[i];
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static long ReadEnum(FieldType type, JsonNode value)
    {
        if (value.GetValueKind() == JsonValueKind.Number) return (long)ReadNumber(value);
        if (value.GetValueKind() == JsonValueKind.String)
        {
            var name = value.GetValue<string>();
            var names = Enum.GetNames(type.Type);
            var values = Enum.GetValuesAsUnderlyingType(type.Type);
            for (var i = 0; i < names.Length; i++)
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt64(values.GetValue(i), CultureInfo.InvariantCulture);
        }

        throw new InvalidDataException($"'{value.ToJsonString()}' is not a {type.Type.Name}.");
    }

    private static Entity ReadEntity(JsonNode value, Func<string, Entity?> findEntity)
    {
        if (value.GetValueKind() != JsonValueKind.String)
            throw new InvalidDataException($"Expected an object name, got {value.ToJsonString()}.");
        var name = value.GetValue<string>();
        return findEntity(name) ?? throw new InvalidDataException($"No object is named '{name}'.");
    }

    private static bool ReadBool(JsonNode value) => value.GetValueKind() switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => ReadNumber(value) != 0,
        _ => throw new InvalidDataException($"Expected true or false, got {value.ToJsonString()}.")
    };

    // Parsed from text so a value built in code as an int reads the same as one loaded from a file.
    private static double ReadNumber(JsonNode value)
    {
        if (value.GetValueKind() == JsonValueKind.Number &&
            double.TryParse(value.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return number;
        throw new InvalidDataException($"Expected a number, got {value.ToJsonString()}.");
    }

    private static void ReadNumbers(JsonNode value, Span<float> target, int count)
    {
        if (value is not JsonArray array || array.Count != count)
            throw new InvalidDataException($"Expected {count} numbers, got {value.ToJsonString()}.");
        for (var i = 0; i < count; i++)
            target[i] = (float)ReadNumber(array[i] ?? throw new InvalidDataException("A number is missing."));
    }
}
