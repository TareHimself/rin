using System.Numerics;
using Rin.Engine;
using Rin.Engine.Views;
using Rin.Engine.Views.Graphics;

namespace rin.Examples.ViewsTest;

public class PrettyView : ContentView
{
    private double _createdAt;

    public PrettyView()
    {
        _createdAt = SEngine.Get().GetTimeSeconds();
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

    public override void CollectContent(Matrix4x4 transform, CommandList commands)
    {
        //Angle = (float)(((SRuntime.Get().GetTimeSeconds() * 100.0) - _createdAt) % 360.0);
        commands.Add(new CustomShaderCommand(transform, GetContentSize(),
            Parent?.Parent?.Parent?.IsHovered ?? false, Surface?.GetCursorPosition() ?? Vector2.Zero));
    }
}