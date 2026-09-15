using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Window;

public interface IWindowSurface : ISurface
{
    public IWindow Window { get; }
    public IWindowRenderer Renderer { get; }
}