using rin.Core.Math;
using rin.Widgets;
using rin.Widgets.Graphics;

namespace AudioPlayer.Widgets;

public class AudioVisualizer : ContentWidget
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