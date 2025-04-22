using System.Numerics;
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
    public abstract void CollectContent(Matrix4x4 transform, CommandList commands);

    public override void Collect(in Matrix4x4 transform, in Rect clip, CommandList cmds)
    {
        if (!IsVisible) return;

        CollectContent(transform.Translate(new Vector2(Padding.Left, Padding.Top)), cmds);
    }

    public override void Update(float deltaTime)
    {
        ((IAnimatable)this).UpdateRunner();
        base.Update(deltaTime);
    }
}