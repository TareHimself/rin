using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Content;

/// <summary>
/// For issuing arbitrary draw commands
/// </summary>
public class Canvas : ContentView
{
    public required Action<Canvas,Matrix3, DrawCommands> Paint { get; init; }
    protected override Vector2<float> ComputeDesiredContentSize() => 0.0f;

    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        Paint.Invoke(this,transform,drawCommands);
    }
}