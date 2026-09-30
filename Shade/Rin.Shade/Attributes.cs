using System;

namespace Rin.Shade;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ShaderAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}

/// <summary>
/// Marks the compute entry-point method and its thread-group size in one place - a compute shader
/// has no valid state where you'd want one without the other, so splitting them bought nothing and
/// cost a real bug: an override that didn't repeat a separate [NumThreads] silently fell back to
/// (1,1,1) instead of inheriting the base's value.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ComputeAttribute(int x, int y, int z) : Attribute
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Z { get; } = z;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class VertexAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class FragmentAttribute : Attribute;

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

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
public sealed class AttachmentAttribute(AttachmentFormat format) : Attribute
{
    public AttachmentFormat Format { get; } = format;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class DepthAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class StencilAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field)]
public sealed class PushAttribute : Attribute;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class ShaderStructAttribute : Attribute;

/// <summary>
/// Marks a type meant to be derived from or called into by a downstream assembly's shaders - a
/// shader base class, or a stdlib-style helper class/struct. Rin.Shade.SourceGenerator finds every
/// marked declaration and generates a const string holding its containing file's exact source text,
/// so a downstream transpile can walk its body without needing this project's actual .cs files -
/// see ScratchCompilationBuilder in Rin.Shade.Transpiler for the consumer side.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ShadeExportAttribute : Attribute;

/// <summary>
/// Marks an assembly as having generated exported-shader-source constants (see
/// Rin.Shade.Generated.ShaderSourceContainer) for a downstream consumer's transpile to read.
/// Emitted automatically by Rin.Shade.SourceGenerator when it finds at least one
/// [ShadeExport]-marked declaration - never written by hand.
/// AllowMultiple: a scratch compilation combines generated source from several upstream assemblies,
/// each of which independently emits this attribute in its own generated file - without
/// AllowMultiple, combining two into one compilation would be a duplicate-attribute error (CS0579)
/// purely as an artifact of the mechanism, not a real conflict.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ShaderSourcesAttribute : Attribute;

/// <summary>
/// Marks a static field of a [ShaderStruct] type as a group of resources that must land in one
/// descriptor set together - lowers to a Slang ParameterBlock&lt;T&gt;, whose set/binding Slang
/// assigns on its own. No explicit numbers: unlike the old ShaderBindingAttribute this replaces,
/// there is nothing here for shader-side and engine-side code to disagree about by convention.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class BindingGroupAttribute : Attribute;

/// <summary>
/// Marks a struct as the shape of a named, engine-owned resource table (the global bindless pool).
/// A static field of this type is lowered to its own parameter block, like a [BindingGroup], and the
/// name travels into reflection so the backend can bind its own registered descriptor set there
/// instead of building one from the shader.
/// </summary>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class BindlessBlockAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class SemanticAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangExpressionAttribute(string template) : Attribute
{
    public string Template { get; } = template;
    public int Precedence { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangStatementAttribute(string template) : Attribute
{
    public string Template { get; } = template;
}
