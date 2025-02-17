using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Graphics;

namespace misc.VectorRendering;

public class PathRenderer : ContentView
{
    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return Vector2.Zero;
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        
    }
}