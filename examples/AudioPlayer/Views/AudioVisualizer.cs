using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Graphics;

namespace AudioPlayer.Views;

public class AudioVisualizer : ContentView
{
    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return availableSpace;
    }

    protected override Vec2<float> ComputeDesiredContentSize()
    {
        throw new NotImplementedException();
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        throw new NotImplementedException();
    }
}