using System.Numerics;
using Rin.Core.Animation;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.Core.Views;
using Rin.Core.Views.Animation;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Graphics;

namespace Examples.ViewsTest;

public class WrapContainer : ButtonView
{
    private readonly View _content;

    public WrapContainer(View content)
    {
        Color = Color.Transparent;
        _content = content;
        var sizer = new SizerView
        {
            WidthOverride = ViewsTestExample.TileSize,
            HeightOverride = ViewsTestExample.TileSize,
            InitChild = content,
            Padding = 10.0f
        };
        InitChild = sizer;
        content.Pivot = new Vector2(0.5f);
        OnReleased += (@event, button) =>
        {
            //_content.StopAll().RotateTo(360,0.5f).After().Do(() => _content.Angle = 0.0f);
            var transitionDuration = 0.8f;
            var method = EasingFunctions.EaseInOutCubic;
            // sizer
            //     .StopAll()
            //     .WidthTo(ViewsTestExample.TileSize * 4f + 60.0f, transitionDuration, easingFunction: method)
            //     .HeightTo(ViewsTestExample.TileSize * 2f + 10.0f, transitionDuration, easingFunction: method)
            //     .Delay(4)
            //     .WidthTo(ViewsTestExample.TileSize, transitionDuration, easingFunction: method)
            //     .HeightTo(ViewsTestExample.TileSize, transitionDuration, easingFunction: method);
            content.StopAll().RotateTo(45, 2).ScaleTo(new Vector2(2), 2).After().RotateTo(0, 2)
                .ScaleTo(new Vector2(1), 2);
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

    public override void Collect(in Matrix4x4 transform, in Rect2D clip, CommandList commands)
    {
        base.Collect(transform, clip, commands);
    }
}