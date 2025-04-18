﻿using System.Numerics;
using Rin.Engine.Animation;
using Rin.Engine.Math;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views;

public abstract class ContentView : View
{
    /// <summary>
    ///     Collect Draw commands from this view while accounting for padding offsets
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="commands"></param>
    public abstract void CollectContent(Matrix4x4 transform, PassCommands commands);

    public override void Collect(Matrix4x4 transform, Rect clip, PassCommands passCommands)
    {
        if (!IsVisible) return;

        CollectContent(transform.Translate(new Vector2(Padding.Left, Padding.Top)), passCommands);
    }

    public override void Update(float deltaTime)
    {
        ((IAnimatable)this).UpdateRunner();
        base.Update(deltaTime);
    }
}