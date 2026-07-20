using Rin.Core.Views;
using Rin.Core.Views.Composite;

namespace ChatApp.Views;

public class SidePanel : SizerView
{
    public SidePanel()
    {
        InitChild = new RectView
        {
            Color = Color.Red
        };
        WidthOverride = 200;
    }
}