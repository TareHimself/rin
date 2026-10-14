using Examples.Paint.Views;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;

namespace Examples.Paint;

public sealed class PaintExample : Example
{
    public override string Name => "paint";

    public override string Title => "Paint";

    public override Extent2D WindowSize => new(1100, 760);

    public override void Start(ExampleContext context)
    {
        var paint = context.Add(new PaintView());
        context.OnKey(e =>
        {
            if (e is { Key: InputKey.Z, State: InputState.Pressed, IsControlDown: true }) paint.Canvas.Undo();
        });
    }
}
