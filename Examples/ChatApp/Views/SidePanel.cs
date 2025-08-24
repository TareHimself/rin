using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rect = Rin.Framework.Views.Composite.Rect;

namespace ChatApp.Views;

public class SidePanel : Sizer
{
    public SidePanel()
    {
        Child = new Rect
        {
            Color = Color.Red
        };
        WidthOverride = 200;
    }
}