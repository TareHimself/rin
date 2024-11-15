using rin.Core.Math;
using rin.Widgets;
using rin.Widgets.Graphics;

namespace WidgetTest;

public class PrettyWidget : Widget
{
    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        drawCommands.Add(new PrettyShaderDrawCommand(info.Transform,info.Size,Parent?.Parent?.IsHovered ?? false));
    }
}