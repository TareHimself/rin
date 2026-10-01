
namespace Rin.Shade;

public interface IShaderDescriptor
{
    string Path { get; }
}

public interface IComputeDescriptor : IShaderDescriptor
{
    (uint X, uint Y, uint Z) ThreadGroupSize { get; }
}

public interface IGraphicsDescriptor : IShaderDescriptor
{
    AttachmentFormat[] AttachmentFormats { get; }
    BlendState BlendState { get; }
    bool UsesDepth { get; }
    bool UsesStencil { get; }
}

public sealed record ComputeDescriptor(string Path, (uint X, uint Y, uint Z) ThreadGroupSize) : IComputeDescriptor;

public record GraphicsDescriptor(
    string Path,
    AttachmentFormat[] AttachmentFormats,
    BlendState BlendState,
    bool UsesDepth,
    bool UsesStencil) : IGraphicsDescriptor;
