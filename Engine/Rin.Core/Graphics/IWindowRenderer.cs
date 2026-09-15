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
}