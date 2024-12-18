
using rin.Framework.Core.Math;
using rin.Framework.Views;
using rin.Framework.Views.Graphics;

namespace AudioPlayer.Widgets;

public class AudioVisualizer : ContentView
{
    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        throw new NotImplementedException();
    }

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        throw new NotImplementedException();
    }
}