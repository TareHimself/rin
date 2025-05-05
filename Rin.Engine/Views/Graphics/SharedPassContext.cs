using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Math;

namespace Rin.Engine.Views.Graphics;

public class SharedPassContext(in Extent2D extent)
{
    public Extent2D Extent = extent;

    public Matrix4x4 ProjectionMatrix = MathR.ViewportProjection(extent.Width, extent.Height, 0.0f, 1.0f);

    public FrameStats Stats;
    public uint MainImageId { get; set; }
    public uint StencilImageId { get; set; }
    public uint CopyImageId { get; set; }
}