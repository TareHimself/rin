
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;

namespace WidgetTest;

public class PrettyView : ContentView
{
    private double _createdAt = 0.0f;
    public PrettyView()
    {
        _createdAt = SRuntime.Get().GetTimeSeconds();
    }
    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        //Translate = availableSpace / 2.0f;
        return availableSpace;
    }

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        return 0.0f;
    }

    public override void CollectContent(Mat3 transform, DrawCommands drawCommands)
    {
        //Angle = (float)(((SRuntime.Get().GetTimeSeconds() * 100.0) - _createdAt) % 360.0);
        drawCommands.Add(new PrettyShaderDrawCommand(transform,GetContentSize(),Parent?.Parent?.Parent?.IsHovered ?? false));
    }
}