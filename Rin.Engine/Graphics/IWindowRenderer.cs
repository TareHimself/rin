using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Windows;

namespace Rin.Engine.Graphics;

public interface IWindowRenderer : IRenderer
{
    public event Action<IGraphBuilder>? OnCollect;

    public IWindow GetWindow();
}