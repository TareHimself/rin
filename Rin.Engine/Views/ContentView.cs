using System.Numerics;
using Rin.Engine.Core.Animation;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views;

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
        if (!IsVisible) return;

        CollectContent(transform.Translate(new Vector2(Padding.Left, Padding.Top)), passCommands);
    }

    public override void Update(float deltaTime)
    {
        ((IAnimatable)this).UpdateRunner();
        base.Update(deltaTime);
    }
}