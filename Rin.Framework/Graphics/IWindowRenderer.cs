using Rin.Framework.Graphics.FrameGraph;
using Rin.Framework.Graphics.Windows;

namespace Rin.Framework.Graphics;

public interface IWindowRenderer : IRenderer
{
    public event Action<IGraphBuilder>? OnCollect;

    public IWindow GetWindow();

    public Extent2D GetRenderExtent();
    public bool VsyncEnabled { get; }
    public void SetVsyncEnabled(bool enabled);
}