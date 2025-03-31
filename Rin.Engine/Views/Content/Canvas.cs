using System.Numerics;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Content;

/// <summary>
///     For issuing arbitrary draw commands
/// </summary>
public class Canvas : ContentView
{
    public required Action<Canvas, Matrix4x4, PassCommands> Paint { get; init; }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(Matrix4x4 transform, PassCommands commands)
    {
        Paint.Invoke(this, transform, commands);
    }
}