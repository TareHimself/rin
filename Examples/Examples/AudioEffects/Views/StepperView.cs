using Examples.Common.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.AudioEffects.Views;

/// <summary>
///     Minus and plus buttons around a value. The step callback takes -1, 0 or 1 and returns the text to show.
/// </summary>
public sealed class StepperView : FlexBoxView
{
    private const float ButtonWidth = 34f;
    private const float ValueWidth = 120f;

    private readonly Func<int, string> _step;
    private readonly TextBoxView _value;

    public StepperView(Func<int, string> step)
    {
        _step = step;
        _value = Ui.Label(step(0), 13f, Ui.Muted);
        Axis = Axis.Row;
        InitSlots =
        [
            Controls.Fixed(Controls.Compact("-", () => Step(-1)), ButtonWidth),
            Ui.Slot(Ui.Gap(0f, 8f)),
            Controls.Fixed(_value, ValueWidth),
            Controls.Fixed(Controls.Compact("+", () => Step(1)), ButtonWidth)
        ];
    }

    private void Step(int direction)
    {
        _value.Content = _step(direction);
    }
}
