using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Windows;

namespace Rin.Framework.Graphics;

public interface IWindowRenderer : IRenderer
{
    public bool VsyncEnabled { get; }
    public event Action<IGraphCollector>? OnCollect;

    public IWindow GetWindow();

    public Extent2D GetRenderExtent();
    public void SetVsyncEnabled(bool enabled);
}