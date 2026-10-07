using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Window;

namespace Examples;

public sealed record ExampleContext(ExampleHost Application, IWindowSurface Surface, string[] Args)
{
    public IWindow Window => Surface.Window;
}
