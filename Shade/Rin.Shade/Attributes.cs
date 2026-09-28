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

[AttributeUsage(AttributeTargets.Field)]
public sealed class FixedSizeAttribute(int size) : Attribute
{
    public int Size { get; } = size;
}

/// <summary>
/// Marks a static field as a Vulkan-bound resource - a single resource type, or an array of one
/// for a bindless slot. Set/Binding are declared and owned here, never compiler-assigned.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ShaderBindingAttribute : Attribute
{
    public int Set { get; init; }
    public int Binding { get; init; }
    public bool UpdateAfterBind { get; init; }
    public bool Partial { get; init; }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class SemanticAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangCallAttribute(string template) : Attribute
{
    public string Template { get; } = template;
    public string? Header { get; init; }
    public int Precedence { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangStatementAttribute(string template) : Attribute
{
    public string Template { get; } = template;
    public string? Header { get; init; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SlangBodyAttribute(string body) : Attribute
{
    public string Body { get; } = body;
    public string? Header { get; init; }
}
