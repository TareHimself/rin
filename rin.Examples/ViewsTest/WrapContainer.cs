using System.Numerics;
using Rin.Framework.Animation;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views;
using Rin.Framework.Views.Animation;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Graphics;

namespace rin.Examples.ViewsTest;

public class WrapContainer : ButtonView
{
    private readonly View _content;

    public WrapContainer(View content)
    {
        Color = Color.Transparent;
        _content = content;
        var sizer = new SizerView
        {
            WidthOverride = ViewsTestApplication.TileSize,
            HeightOverride = ViewsTestApplication.TileSize,
            Child = new RectView
            {
                Child = content,
                Color = Color.Red
            },
            Padding = 10.0f
        };
        Child = sizer;
        content.Pivot = new Vector2(0.5f);
        OnReleased += (@event, button) =>
        {
            //_content.StopAll().RotateTo(360,0.5f).After().Do(() => _content.Angle = 0.0f);
            var transitionDuration = 0.8f;
            var method = EasingFunctions.EaseInOutCubic;
            // sizer
            //     .StopAll()
            //     .WidthTo(ViewsTestApplication.TileSize * 4f + 60.0f, transitionDuration, easingFunction: method)
            //     .HeightTo(ViewsTestApplication.TileSize * 2f + 10.0f, transitionDuration, easingFunction: method)
            //     .Delay(4)
            //     .WidthTo(ViewsTestApplication.TileSize, transitionDuration, easingFunction: method)
            //     .HeightTo(ViewsTestApplication.TileSize, transitionDuration, easingFunction: method);
            content.StopAll().RotateTo(45,2).ScaleTo(new Vector2(2),2).After().RotateTo(0,2).ScaleTo(new Vector2(1),2);
        };
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        var size = base.LayoutContent(availableSpace);
        _content.Offset = _content.GetSize() * 0.5f + new Vector2(Padding.Left, Padding.Top);
        //_content.Offset = _content.GetSize() * .5f;
        return size;
    }

    protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        base.CollectSelf(transform, cmds);
    }

    public override void Collect(in Matrix4x4 transform, in Rin.Framework.Graphics.Rect2D clip, CommandList commands)
    {
        base.Collect(transform, clip, commands);
    }
}