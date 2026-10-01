
namespace Rin.Shade;

/// <summary>
/// Identifies a compiled shader.
/// </summary>
public interface IShaderDescriptor
{
    /// <summary>
    /// Path of the compiled shader binary.
    /// </summary>
    string Path { get; }
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
public sealed record ComputeDescriptor(string Path, (uint X, uint Y, uint Z) ThreadGroupSize) : IComputeDescriptor;

/// <summary>
/// Plain-data <see cref="IGraphicsDescriptor" />.
/// </summary>
public record GraphicsDescriptor(
    string Path,
    AttachmentFormat[] AttachmentFormats,
    BlendState BlendState,
    bool UsesDepth,
    bool UsesStencil) : IGraphicsDescriptor;
