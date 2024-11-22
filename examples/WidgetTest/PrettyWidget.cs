using rin.Core;
using rin.Core.Animation;
using rin.Core.Math;
using rin.Graphics.Windows;
using rin.Widgets;
using rin.Widgets.Animation;
using rin.Widgets.Events;
using rin.Widgets.Graphics;

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