
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