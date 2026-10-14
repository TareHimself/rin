using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace Examples;

/// <summary>
///     A button in the bottom left corner that returns to the launcher.
/// </summary>
public sealed class BackButtonView : PanelView
{
    private const float Width = 96f;
    private const float Margin = 16f;

    public BackButtonView(Action onBack)
    {
        Visibility = Visibility.VisibleNoHitTestSelf;
        InitSlots =
        [
            new PanelSlot
            {
                Child = new SizerView { WidthOverride = Width, InitChild = Ui.QuietButton("Back", onBack) },
                MinAnchor = new Vector2(0f, 1f),
                MaxAnchor = new Vector2(0f, 1f),
                Alignment = new Vector2(0f, 1f),
                Offset = new Vector2(Margin, -Margin),
                SizeToContent = true
            }
        ];
    }
}
