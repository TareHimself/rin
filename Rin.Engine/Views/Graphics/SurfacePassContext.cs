using System.Numerics;
using Rin.Engine.Graphics;
using Rin.Engine.Math;

namespace Rin.Engine.Views.Graphics;

public class SurfacePassContext(in Extent2D extent)
{
    /// <summary>
    /// The render extent of the <see cref="MainImageId"/> , <see cref="StencilImageId"/> and <see cref="CopyImageId"/>
    /// </summary>
    public Extent2D Extent = extent;

    /// <summary>
    /// The projection matrix that was used to render this surface
    /// </summary>
    public Matrix4x4 ProjectionMatrix = MathR.ViewportProjection(extent.Width, extent.Height, 0.0f, 1.0f);

    public FrameStats Stats;
    
    /// <summary>
    /// The id of the image that the surface was rendered to
    /// </summary>
    public uint MainImageId { get; set; }
    
    /// <summary>
    /// The id of the stencil image that was used to render the main image
    /// </summary>
    public uint StencilImageId { get; set; }
    /// <summary>
    /// The id of the copy image that is used for tasks that require some kind of temp copy
    /// </summary>
    public uint CopyImageId { get; set; }
}