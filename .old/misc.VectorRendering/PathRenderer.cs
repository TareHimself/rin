using System.Numerics;
using Rin.Framework.Views;
using Rin.Framework.Views.Graphics;

namespace misc.VectorRendering;

public class PathRenderer : ContentView
{
    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return Vector2.Zero;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
    }
}