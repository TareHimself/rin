﻿using System.Numerics;
using rin.Framework.Core.Animation;
using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views;

public abstract class ContentView : View
{
    /// <summary>
    ///     Collect Draw commands from this view while accounting for padding offsets
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="commands"></param>
    public abstract void CollectContent(Mat3 transform, PassCommands commands);

    public override void Collect(Mat3 transform, Rect clip, PassCommands passCommands)
    {
        ((IAnimatable)this).Update();

        if (!IsVisible) return;

        CollectContent(transform.Translate(new Vector2(Padding.Left, Padding.Top)), passCommands);
    }
}