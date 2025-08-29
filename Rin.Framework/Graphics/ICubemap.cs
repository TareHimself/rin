using Rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

public interface ICubemap : IGraphResource
{
    public ImageFormat Format { get; }
    public Extent2D SliceSize { get; }
}