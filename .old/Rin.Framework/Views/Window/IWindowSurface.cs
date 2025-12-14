using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Window;

public interface IWindowSurface : ISurface
{
    public IWindow Window { get; }
    public IWindowRenderer Renderer { get; }
}