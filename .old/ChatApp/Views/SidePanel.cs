using Rin.Framework.Views;
using Rin.Framework.Views.Composite;

namespace ChatApp.Views;

public class SidePanel : SizerView
{
    public SidePanel()
    {
        Child = new RectView
        {
            Color = Color.Red
        };
        WidthOverride = 200;
    }
}