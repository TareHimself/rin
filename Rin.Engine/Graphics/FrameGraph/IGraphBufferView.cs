namespace Rin.Engine.Graphics.FrameGraph;

/// <summary>
/// Not currently in use
/// </summary>
public interface IGraphBufferView : IDeviceBufferView
{
    /// <summary>
    ///     True if this image is owned or was created by the graph
    /// </summary>
    public bool Owned { get; }
}