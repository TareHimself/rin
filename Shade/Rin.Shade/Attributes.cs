using System;

namespace Rin.Shade;

/// <summary>
/// Marks a class as a shader and gives the path its compiled result is registered under.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ShaderAttribute(string path) : Attribute
{
    /// <summary>
    /// The shader's path, for example <c>Shaders/Rin/Core/Views/blur.slang</c>.
    /// </summary>
    public string Path { get; } = path;
}

/// <summary>
/// Marks the compute entry-point method and its thread-group size in one place. A compute shader has
/// no valid state with one but not the other, and keeping them together means an override can never
/// silently fall back to (1, 1, 1) instead of inheriting the base class's size.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ComputeAttribute(int x, int y, int z) : Attribute
{
    /// <summary>
    /// Thread-group size along X.
    /// </summary>
    public int X { get; } = x;

    /// <summary>
    /// Thread-group size along Y.
    /// </summary>
    public int Y { get; } = y;

    /// <summary>
    /// Thread-group size along Z.
    /// </summary>
    public int Z { get; } = z;
}

/// <summary>
/// Marks the vertex entry-point method of a graphics shader.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class VertexAttribute : Attribute;

/// <summary>
/// Marks the fragment entry-point method of a graphics shader.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class FragmentAttribute : Attribute;

/// <summary>
/// The pixel format of a color attachment a fragment shader writes.
/// </summary>
public enum AttachmentFormat
{
    R8,
    R16,
    R32,
    RG8,
    RG16,
    RG32,
    RGBA8,
    RGBA16,
    RGBA32
}

/// <summary>
/// Declares the format of a color attachment. On a fragment method it describes the single color output;
/// on a field of the returned struct it describes that field's attachment.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
public sealed class AttachmentAttribute(AttachmentFormat format) : Attribute
{
    /// <summary>
    /// The attachment's pixel format.
    /// </summary>
    public AttachmentFormat Format { get; } = format;
}

/// <summary>
/// Marks a shader stage as using the depth attachment.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DepthAttribute : Attribute;

/// <summary>
/// Marks a shader stage as using the stencil attachment.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StencilAttribute : Attribute;

/// <summary>
/// Marks the field that holds a shader's push constants. It lowers to a push-constant uniform named <c>push</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class PushAttribute : Attribute;

/// <summary>
/// Marks a struct as shader data. The transpiler lowers any struct a shader reaches, so this attribute
/// currently has no effect on emission.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class ShaderStructAttribute : Attribute;

/// <summary>
/// Marks a type meant to be derived from or called into by a downstream assembly's shaders: a shader
/// base class, or a helper class or struct. Rin.Shade.SourceGenerator generates a const string holding
/// the containing file's exact source text for every marked declaration, so a downstream transpile can
/// walk its bodies without this project's .cs files. See ScratchCompilationBuilder in Rin.Shade.Transpiler
/// for the consumer side.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ShadeExportAttribute : Attribute;

/// <summary>
/// Marks an assembly as having generated exported-shader-source constants (see
/// <c>Rin.Shade.Generated.ShaderSourceContainer</c>) for a downstream transpile to read. Rin.Shade.SourceGenerator
/// emits it when it finds at least one <see cref="ShadeExportAttribute"/> declaration, so it is never written by hand.
/// </summary>
/// <remarks>
/// <c>AllowMultiple</c> is set because a scratch compilation combines generated source from several upstream
/// assemblies, each of which emits this attribute in its own generated file. Without it, combining two would be
/// a duplicate-attribute error (CS0579) that comes from the mechanism and not from a real conflict.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ShaderSourcesAttribute : Attribute;

/// <summary>
/// Marks a static field as a group of resources that must land in one descriptor set together. It lowers
/// to a Slang <c>ParameterBlock&lt;T&gt;</c>, whose set and binding Slang assigns itself, so there are no
/// explicit numbers for shader-side and engine-side code to disagree about.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class BindingGroupAttribute : Attribute;

/// <summary>
/// Marks a struct as the shape of a named, engine-owned resource table such as the global bindless pool.
/// A static field of this type lowers to its own parameter block, like a <see cref="BindingGroupAttribute"/>,
/// and the name travels into reflection so the backend can bind its own registered descriptor set there
/// instead of building one from the shader.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BindlessBlockAttribute(string name) : Attribute
{
    /// <summary>
    /// The name the backend registers the engine's descriptor set under.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Gives a field of a stage input or output struct a Slang semantic. Derive from this class to define a named
/// semantic; the transpiler reads the semantic from a <c>const string SemanticName</c> on the derived class.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SemanticAttribute(string name) : Attribute
{
    /// <summary>
    /// The semantic, for example <c>SV_Position</c>.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// The <c>SV_Position</c> semantic: the clip-space position from a vertex stage, or the pixel position in a fragment stage.
/// </summary>
public sealed class PositionAttribute() : SemanticAttribute(SemanticName)
{
    public const string SemanticName = "SV_Position";
}

/// <summary>
/// The <c>SV_VertexID</c> semantic.
/// </summary>
public sealed class VertexIdAttribute() : SemanticAttribute(SemanticName)
{
    public const string SemanticName = "SV_VertexID";
}

/// <summary>
/// The <c>SV_InstanceID</c> semantic.
/// </summary>
public sealed class InstanceIdAttribute() : SemanticAttribute(SemanticName)
{
    public const string SemanticName = "SV_InstanceID";
}

/// <summary>
/// The <c>SV_DispatchThreadID</c> semantic of a compute shader.
/// </summary>
public sealed class DispatchThreadIdAttribute() : SemanticAttribute(SemanticName)
{
    public const string SemanticName = "SV_DispatchThreadID";
}

/// <summary>
/// The <c>SV_GroupID</c> semantic of a compute shader.
/// </summary>
public sealed class GroupIdAttribute() : SemanticAttribute(SemanticName)
{
    public const string SemanticName = "SV_GroupID";
}

/// <summary>
/// The <c>SV_GroupThreadID</c> semantic of a compute shader.
/// </summary>
public sealed class GroupThreadIdAttribute() : SemanticAttribute(SemanticName)
{
    public const string SemanticName = "SV_GroupThreadID";
}

/// <summary>
/// The <c>SV_Target</c> semantic for a fragment output. The index is appended, so <c>Target(1)</c> is <c>SV_Target1</c>.
/// </summary>
public sealed class TargetAttribute(int index) : SemanticAttribute(SemanticName + index)
{
    public const string SemanticName = "SV_Target";
}

/// <summary>
/// Binds a method that has no shader body to a Slang expression. Placeholders: <c>@0</c>, <c>@1</c>, ...
/// are the arguments in parameter order (the receiver is <c>@0</c> for an extension method), <c>@name</c> is the
/// argument of the parameter with that name, <c>@T0</c>, <c>@T1</c>, ... are the generic type arguments, and
/// <c>@this</c> is the instance the method is called on. Use <c>$"@{nameof(parameter)}"</c> so a rename
/// updates the template. An unknown placeholder is a diagnostic.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangExpressionAttribute(string template) : Attribute
{
    /// <summary>
    /// The Slang expression with placeholders.
    /// </summary>
    public string Template { get; } = template;

    /// <summary>
    /// Reserved. The transpiler decides where to add parentheses from the template itself and does not read this.
    /// </summary>
    public int Precedence { get; init; }
}

/// <summary>
/// Binds a method that has no shader body to a whole Slang statement, for operations such as <c>discard;</c>
/// that are not expressions. It uses the same placeholders as <see cref="SlangExpressionAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangStatementAttribute(string template) : Attribute
{
    /// <summary>
    /// The Slang statement with placeholders.
    /// </summary>
    public string Template { get; } = template;
}
