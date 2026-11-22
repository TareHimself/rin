namespace Rin.World;

/// <summary>
///     What a <see cref="FieldType" /> holds, so tools can read, draw and convert a field without its C# type.
/// </summary>
public enum FieldKind
{
    Bool,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float32,
    Float64,
    Vector2,
    Vector3,
    Vector4,
    Quaternion,
    Matrix4x4,
    Entity,
    Enum,

    /// <summary>
    ///     A struct described by its <see cref="FieldType.Fields" />.
    /// </summary>
    Struct,

    /// <summary>
    ///     Bytes the schema does not describe, allowed only for <see cref="EcsProcessLocalAttribute" /> fields.
    /// </summary>
    Opaque
}
