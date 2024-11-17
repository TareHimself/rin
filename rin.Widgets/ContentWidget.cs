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

    public override void Collect(TransformInfo info, DrawCommands drawCommands)
    {
        if (!IsVisible)
        {
            return;
        }
        
        var transform = info.Transform * Matrix3.Identity.Translate(new Vector2<float>(Padding.Left,Padding.Top));
        
        CollectContent(transform, drawCommands);
    }
}