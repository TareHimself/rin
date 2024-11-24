﻿using rin.Core.Animation;
using rin.Core.Math;
using rin.Widgets.Graphics;

namespace rin.Widgets;

public abstract class ContentWidget : Widget
{
    /// <summary>
    /// Collect Draw commands from this widget while accounting for padding offsets
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="drawCommands"></param>
    public abstract void CollectContent(Matrix3 transform,DrawCommands drawCommands);

    public override void Collect(Matrix3 transform, Rect clip, DrawCommands drawCommands)
    {
        ((IAnimatable)this).Update();
        
        if (!IsVisible)
        {
            return;
        }
        
        CollectContent(transform.Translate(new Vector2<float>(Padding.Left,Padding.Top)), drawCommands);
    }
}