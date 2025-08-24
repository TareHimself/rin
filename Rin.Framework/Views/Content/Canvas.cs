using System.Numerics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views.Content;

/// <summary>
///     For issuing arbitrary draw commands
/// </summary>
public class Canvas : ContentView
{
    public required Action<Canvas, Matrix4x4, CommandList> Paint { get; init; }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        Paint.Invoke(this, transform, commands);
    }
}