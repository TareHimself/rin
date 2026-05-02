using System.Numerics;
using Rin.Framework.Views;
using Rin.Framework.Views.Graphics;

namespace NodeGraphTest;

public class SpacerView : ContentView
{
    public float VerticalSpacing { get; set; }
    public float HorizontalSpacing { get; set; }

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