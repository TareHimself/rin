using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Rin.Framework.Math;
using Rin.Framework.Animation;
using Rin.Framework.Graphics;
using Rin.Framework.Views.Graphics;

namespace Rin.Framework.Views;

public abstract class ContentView : View
{
    /// <summary>
    ///     Collect Draw commands from this view while accounting for padding offsets
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="commands"></param>
    public abstract void CollectContent(in Matrix4x4 transform, CommandList commands);

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        if (!IsVisible) return;

        CollectContent(transform.Translate(new Vector2(Padding.Left, Padding.Top)), commands);
    }

    public override void Update(float deltaTime)
    {
        ((IAnimatable)this).UpdateRunner();
        base.Update(deltaTime);
    }
}