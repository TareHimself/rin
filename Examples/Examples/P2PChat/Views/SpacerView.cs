using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;

namespace Examples.P2PChat.Views;

public sealed class SpacerView : ContentView
{
    public float VerticalSpacing { get; init; }
    public float HorizontalSpacing { get; init; }

    public override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2(HorizontalSpacing, VerticalSpacing);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ComputeDesiredContentSize();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
    }
}
