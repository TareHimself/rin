using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Views;
using Rin.Engine.Views.Graphics;

namespace rin.Examples.AudioPlayer.Views;

public class AudioVisualizer : ContentView
{
    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        throw new NotImplementedException();
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        throw new NotImplementedException();
    }
}