using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets.Content;

/// <summary>
/// For issuing arbitrary draw commands
/// </summary>
public class Canvas : Widget
{
    public required Action<Canvas, TransformInfo, DrawCommands> Paint { get; init; }
    protected override Vector2<float> ComputeDesiredContentSize() => 0.0f;

    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        Paint.Invoke(this,info,drawCommands);
    }
}