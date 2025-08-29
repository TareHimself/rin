using Rin.Framework.Graphics.Textures;

namespace Rin.Framework.Graphics.FrameGraph;

public interface IGraphImage : IImage2D
{
    // /// <summary>
    // ///     The current layout of this graph image
    // /// </summary>
    // public ImageLayout Layout { get; set; }

    /// <summary>
    ///     The image handle if this image was marked for read
    /// </summary>
    public ImageHandle BindlessHandle { get; }
}