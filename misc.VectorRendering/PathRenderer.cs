using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Graphics;

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