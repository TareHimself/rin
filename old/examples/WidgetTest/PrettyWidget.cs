
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Widgets;
using rin.Framework.Widgets.Animation;
using rin.Framework.Widgets.Events;
using rin.Framework.Widgets.Graphics;

namespace WidgetTest;

public class PrettyWidget : ContentWidget
{
    private double _createdAt = 0.0f;
    public PrettyWidget()
    {
        _createdAt = SRuntime.Get().GetTimeSeconds();
    }
    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        Translate = availableSpace / 2.0f;
        return availableSpace;
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        //Angle = (float)(((SRuntime.Get().GetTimeSeconds() * 100.0) - _createdAt) % 360.0);
        drawCommands.Add(new PrettyShaderDrawCommand(transform,GetContentSize(),Parent?.Parent?.IsHovered ?? false));
    }
}