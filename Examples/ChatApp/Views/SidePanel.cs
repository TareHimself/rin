using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rect = Rin.Engine.Views.Composite.Rect;

namespace ChatApp.Views;

public class SidePanel : Sizer
{
    public SidePanel()
    {
        Padding = new Padding(20.0f);
        Child = new Rect
        {
            Color = Color.Red
        };
        WidthOverride = 300;
    }
}