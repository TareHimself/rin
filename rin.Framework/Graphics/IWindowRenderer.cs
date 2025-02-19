using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Graphics.Windows;

namespace rin.Framework.Graphics;

public interface IWindowRenderer : IRenderer
{
    public event Action<IGraphBuilder>? OnCollect;

    public IWindow GetWindow();
}