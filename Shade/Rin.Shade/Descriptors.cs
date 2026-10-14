using System.IO;
using System.Reflection;

namespace Rin.Shade;

/// <summary>
/// Identifies a compiled shader: the assembly it is embedded in and the full name of its class, which is
/// also the name of the embedded resource, plus <c>.crsh</c>.
/// </summary>
public interface IShaderDescriptor
{
    Assembly Assembly { get; }

    string Name { get; }

    /// <summary>
    /// Opens the embedded shader package.
    /// </summary>
    Stream Open()
    {
        return Assembly.GetManifestResourceStream(Name + ".crsh") ??
               throw new FileNotFoundException($"'{Assembly.GetName().Name}' has no compiled shader '{Name}'");
    }
}

/// <summary>
/// Describes a compute shader.
/// </summary>
public interface IComputeDescriptor : IShaderDescriptor
{
    /// <summary>
    /// Threads per workgroup along each axis.
    /// </summary>
    (uint X, uint Y, uint Z) ThreadGroupSize { get; }
}

/// <summary>
/// Describes a graphics shader and the fixed-function state it needs.
/// </summary>
public interface IGraphicsDescriptor : IShaderDescriptor
{
    AttachmentFormat[] AttachmentFormats { get; }
    BlendState BlendState { get; }
    bool UsesDepth { get; }
    bool UsesStencil { get; }
}

/// <summary>
/// Plain-data <see cref="IComputeDescriptor" />.
/// </summary>
public sealed record ComputeDescriptor(Assembly Assembly, string Name, (uint X, uint Y, uint Z) ThreadGroupSize)
    : IComputeDescriptor;

/// <summary>
/// Plain-data <see cref="IGraphicsDescriptor" />.
/// </summary>
public record GraphicsDescriptor(
    Assembly Assembly,
    string Name,
    AttachmentFormat[] AttachmentFormats,
    BlendState BlendState,
    bool UsesDepth,
    bool UsesStencil) : IGraphicsDescriptor;
