﻿using rin.Framework.Core.Math;
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

    public Vector4<float> BorderRadius { get; set; } = 0.0f;
    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        return availableSpace;
    }

    protected override Vector2<float> ComputeDesiredContentSize() => new Vector2<float>();

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        var size = GetContentSize();
        drawCommands.AddRect(transform, size,color: BackgroundColor, borderRadius: BorderRadius);
        drawCommands.AddRect(transform, size * new Vector2<float>(Math.Clamp(getProgress(),0.0f,1.0f),1.0f),color: ForegroundColor, borderRadius: BorderRadius);
    }
}