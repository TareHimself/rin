using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Windows;

namespace Rin.Core.Graphics;

public interface IWindowRenderer : IRenderer
{
    public bool VsyncEnabled { get; }
    public event Action<IGraphCollector>? OnCollect;

    public IWindow GetWindow();

    public Extent2D GetRenderExtent();

    /// <summary>
    ///     Graph id of the swapchain image acquired for the frame being built, valid from acquire until the graph compiles.
    /// </summary>
    public uint DestinationImageId { get; }
    public void SetVsyncEnabled(bool enabled);

    public uint GetNumFramesInFlight();
    public void SetFramesInFlight(uint framesInFlight);

    /// <summary>
    ///     The true cap on how many frames in flight can actually be requested, imposed by the
    ///     window's presentation surface.
    /// </summary>
    public uint GetMaxTrueFramesInFlight();
}