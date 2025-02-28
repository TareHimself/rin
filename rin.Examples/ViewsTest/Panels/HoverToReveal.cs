using System.Numerics;
using Rin.Engine.Core.Animation;
using Rin.Engine.Views.Animation;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Layouts;

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
            WidthOverride = 110;
            HeightOverride = 400;
            image.BorderRadius = new Vector4(30.0f);
        }

        protected override void OnCursorEnter(CursorMoveEvent e)
        {
            base.OnCursorEnter(e);
            var duration = 0.1f;
            this.StopAll()
                .Transition(_blur.Strength, 0.0f, (c) => _blur.Strength = c, duration)
                .WidthTo(400);
        }

        protected override void OnCursorLeave()
        {
            base.OnCursorLeave();
            var duration = 0.1f;
            this.StopAll()
                .Transition(_blur.Strength,5.0f, (c) => _blur.Strength = c, duration)
                .WidthTo(110);
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