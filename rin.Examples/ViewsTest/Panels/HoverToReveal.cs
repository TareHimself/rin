using System.Numerics;
using Rin.Framework.Animation;
using Rin.Framework.Views.Animation;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Layouts;

namespace rin.Examples.ViewsTest.Panels;

public class HoverToReveal : Panel
{
    private readonly List _items = new()
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

    public void AddImage(Image image)
    {
        _items.Add(new ImageItem(image));
    }

    private class ImageItem : Sizer
    {
        private readonly BackgroundBlur _blur = new();

        public ImageItem(Image image)
        {
            Child = new Overlay
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