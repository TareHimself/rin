namespace Rin.Engine.Graphics.FrameGraph;

public interface IGraphImage : IDeviceImage
{
    /// <summary>
    ///     The current layout of this graph image
    /// </summary>
    public ImageLayout Layout { get; set; }

    /// <summary>
    ///     True if this image is owned or was created by the graph
    /// </summary>
    public bool Owned { get; }
}