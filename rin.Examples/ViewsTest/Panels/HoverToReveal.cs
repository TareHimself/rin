using System.Numerics;
using Rin.Core.Animation;
using Rin.Core.Views.Animation;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Layouts;

namespace rin.Examples.ViewsTest.Panels;

public class HoverToReveal : PanelView
{
    private readonly ListView _items = new()
    {
        Axis = Axis.Row
    };


    public HoverToReveal()
    {
        Slots =
        [
            new PanelSlot
            {
                Child = _items,
                SizeToContent = true,
                Alignment = new Vector2(0.5f),
                MinAnchor = new Vector2(0.5f),
                MaxAnchor = new Vector2(0.5f)
            }
        ];
    }

    public void AddImage(ImageView image)
    {
        _items.Add(new ImageItem(image));
    }

    private class ImageItem : SizerView
    {
        private readonly BackgroundBlurView _blur = new();

        public ImageItem(ImageView image)
        {
            Child = new OverlayView
            {
                Children =
                [
                    image,
                    _blur
                ]
            };
            WidthOverride = 110;
            HeightOverride = 400;
            image.BorderRadius = new Vector4(30.0f);
        }

        protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
        {
            base.OnCursorEnter(e);
            var duration = 0.1f;
            this.StopAll()
                .Transition(_blur.Strength, 0.0f, c => _blur.Strength = c, duration)
                .WidthTo(400);
        }

        protected override void OnCursorLeave()
        {
            base.OnCursorLeave();
            var duration = 0.1f;
            this.StopAll()
                .Transition(_blur.Strength, 5.0f, c => _blur.Strength = c, duration)
                .WidthTo(110);
        }
    }
}