using System.Numerics;
using rin.Framework.Core.Animation;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Composite;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Layouts;

namespace rin.Examples.ViewsTest.Panels;

public class HoverToReveal : Panel
{
    class ImageItem : Sizer
    {
        private BackgroundBlur _blur = new();
        public ImageItem(Image image)
        {
            Child = new Overlay()
            {
                Children = [
                    
                    image,
                    _blur
                ]
            };
            WidthOverride = 250;
            HeightOverride = 600;
            image.BorderRadius = new Vector4(30.0f);
        }

        protected override void OnCursorEnter(CursorMoveEvent e)
        {
            base.OnCursorEnter(e);
            var duration = 0.1;
            this.StopAll()
                .Transition(_blur.Strength, 0.0f, (c) => _blur.Strength = c, duration)
                .WidthTo(600);
        }

        protected override void OnCursorLeave(CursorMoveEvent e)
        {
            base.OnCursorLeave(e);
            var duration = 0.1;
            this.StopAll()
                .Transition(_blur.Strength,5.0f, (c) => _blur.Strength = c, duration)
                .WidthTo(250);
        }
    }

    private List _items = new List()
    {
        Axis = Axis.Row
    };
    public void AddImage(Image image)
    {
        _items.Add(new ImageItem(image));
    }


    public HoverToReveal()
    {
        Slots =
        [
            new PanelSlot()
            {
                Child = _items,
                SizeToContent = true,
                Alignment = new Vector2(0.5f),
                MinAnchor = new Vector2(0.5f),
                MaxAnchor = new Vector2(0.5f)
            }
        ];
    }
}