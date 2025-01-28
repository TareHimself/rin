using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;

namespace rin.Framework.Views.Content;


/// <summary>
/// Simple progress bar implementation
/// </summary>
public class ProgressBar(Func<float> getProgress) : ContentView
{
    public Color BackgroundColor { get; set; } = Color.Red;
    public Color ForegroundColor { get; set; } = Color.White;

    public Vec4<float> BorderRadius { get; set; } = 0.0f;
    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return availableSpace;
    }

    protected override Vec2<float> ComputeDesiredContentSize() => new Vec2<float>();

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        var size = GetContentSize();
        commands.AddRect(transform, size,color: BackgroundColor, borderRadius: BorderRadius);
        commands.AddRect(transform, size * new Vec2<float>(Math.Clamp(getProgress(),0.0f,1.0f),1.0f),color: ForegroundColor, borderRadius: BorderRadius);
    }
}