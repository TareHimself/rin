using rin.Widgets;
using rin.Widgets.Containers;
using rin.Widgets.Content;
using rin.Widgets.Graphics.Quads;

namespace HoYoPlayClone.widgets;

public class GameIcon : Button
{
    public GameIcon()
    {
        Child = new Sizer()
        {
            WidthOverride = 100,
            HeightOverride = 100,
            Child = new Canvas
            {
                Paint = ((canvas, transform, cmds) =>
                {
                    cmds.AddRect(transform, canvas.GetContentSize(), color: Color.Red);
                })
            }
        };
    }
}