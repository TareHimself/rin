using System.Numerics;
using Rin.Core.Animation;
using Rin.Core.Graphics;
using Rin.Core.Views.Graphics;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views;

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

        CollectContent(transform.ApplyBefore(GetLocalContentTransform()), commands);
    }

    public override void Update(float deltaTime)
    {
        ((IAnimatable)this).UpdateRunner();
        base.Update(deltaTime);
    }
}