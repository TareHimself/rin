using System.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Graphics;

namespace rin.Examples.ViewsTest;

public class PrettyView : ContentView
{
    private double _createdAt = 0.0f;
    public PrettyView()
    {
        _createdAt = SRuntime.Get().GetTimeSeconds();
    }
    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        //Translate = availableSpace / 2.0f;
        return availableSpace;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        //Angle = (float)(((SRuntime.Get().GetTimeSeconds() * 100.0) - _createdAt) % 360.0);
        commands.Add(new PrettyShaderDrawCommand(transform,GetContentSize(),Parent?.Parent?.Parent?.IsHovered ?? false));
    }
}