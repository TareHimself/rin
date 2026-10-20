using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace Examples.Common.Views;

internal static class Controls
{
    public static ActionButton Compact(string text, Action onClicked)
    {
        var button = Ui.QuietButton(text, onClicked);
        button.Padding = new Padding(10f, 6f);
        return button;
    }

    public static void Highlight(ActionButton button, bool on)
    {
        if (on)
            button.Restyle(Ui.Accent, Ui.AccentHover, Ui.AccentPressed, Color.White);
        else
            button.Restyle(Ui.Raised, Ui.Border, Ui.Surface, Ui.Text);
    }

    public static FlexBoxSlot Fixed(IView view, float width)
    {
        return Ui.Slot(new SizerView { WidthOverride = width, InitChild = view }, fit: CrossFit.Desired,
            align: CrossAlign.Center);
    }
}
