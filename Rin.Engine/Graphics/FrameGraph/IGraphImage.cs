using Rin.Engine.Graphics.Textures;

namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphImage : IDeviceImage
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