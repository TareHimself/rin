using rin.Framework.Core.Animation;
using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views;

public abstract class ContentView : View
{
    /// <summary>
    /// Collect Draw commands from this widget while accounting for padding offsets
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="drawCommands"></param>
    public abstract void CollectContent(Mat3 transform,DrawCommands drawCommands);

    public override void Collect(Mat3 transform, Rect clip, DrawCommands drawCommands)
    {
        ((IAnimatable)this).Update();
        
        if (!IsVisible)
        {
            return;
        }
        
        CollectContent(transform.Translate(new Vec2<float>(Padding.Left,Padding.Top)), drawCommands);
    }
}