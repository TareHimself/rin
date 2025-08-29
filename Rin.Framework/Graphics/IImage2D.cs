using Rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

public interface IImage2D : IGraphResource
{
    public ImageFormat Format { get; }
    public Extent3D Extent { get; }
}