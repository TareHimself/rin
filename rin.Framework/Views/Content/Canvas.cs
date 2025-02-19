using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Content;

/// <summary>
///     For issuing arbitrary draw commands
/// </summary>
public class Canvas : ContentView
{
    public required Action<Canvas, Mat3, PassCommands> Paint { get; init; }

    protected override Vector2 ComputeDesiredContentSize()
    {
        return new Vector2();
    }

    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        return availableSpace;
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        Paint.Invoke(this, transform, commands);
    }
}