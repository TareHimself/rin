using rin.Core.Math;
using rin.Widgets;
using rin.Widgets.Graphics;

namespace WidgetTest;

public class PrettyWidget : ContentWidget
{
    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        drawCommands.Add(new PrettyShaderDrawCommand(transform,GetContentSize(),Parent?.Parent?.IsHovered ?? false));
    }
}