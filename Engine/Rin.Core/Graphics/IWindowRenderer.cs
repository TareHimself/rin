using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Windows;

namespace Rin.Core.Graphics;

public interface IWindowRenderer : IRenderer
{
    public bool VsyncEnabled { get; }
    public event Action<IGraphCollector>? OnCollect;

    public IWindow GetWindow();

    public Extent2D GetRenderExtent();
    public void SetVsyncEnabled(bool enabled);

    public uint GetNumFramesInFlight();
    public void SetFramesInFlight(uint framesInFlight);

    /// <summary>
    ///     The true cap on how many frames in flight can actually be requested, imposed by the
    ///     window's presentation surface.
    /// </summary>
    public uint GetMaxTrueFramesInFlight();
}