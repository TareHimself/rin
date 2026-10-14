using Examples.ChildWindows.Views;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;

namespace Examples.ChildWindows;

public sealed class ChildWindowsExample : Example
{
    public override string Name => "windows";

    public override string Title => "Windows";

    public override Extent2D WindowSize => new(720, 480);

    public override void Start(ExampleContext context)
    {
        var window = context.Window;
        context.Add(new WindowControlsView(window));
        context.OnKey(e =>
        {
            if (e is { Key: InputKey.Enter, State: InputState.Pressed, IsAltDown: true })
                window.SetFullscreen(!window.IsFullscreen);
        });
    }
}
