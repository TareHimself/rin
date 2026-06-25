using System.Numerics;
using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Content;

/// <summary>
///     For issuing arbitrary draw commands
/// </summary>
public class CanvasView : ContentView
{
    public required Action<CanvasView, Matrix4x4, CommandList> Paint { get; init; }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        Paint.Invoke(this, transform, commands);
    }
}