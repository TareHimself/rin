using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Content;

/// <summary>
/// For issuing arbitrary draw commands
/// </summary>
public class Canvas : ContentView
{
    public required Action<Canvas,Mat3, DrawCommands> Paint { get; init; }
    protected override Vec2<float> ComputeDesiredContentSize() => 0.0f;

    protected override Vec2<float> LayoutContent(Vec2<float> availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(Mat3 transform, DrawCommands drawCommands)
    {
        Paint.Invoke(this,transform,drawCommands);
    }
}