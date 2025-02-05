using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Graphics;

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