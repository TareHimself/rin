using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace Examples.AudioEffects.Views;

/// <summary>
///     An effect's name and description, its extra controls, and an on/off button.
/// </summary>
public sealed class EffectRowView : FlexBoxView
{
    private const float ToggleWidth = 64f;

    private readonly IEffectSlot _slot;
    private readonly ActionButton _toggle;

    public EffectRowView(IEffectSlot slot, string name, string description, IView? control = null)
    {
        _slot = slot;
        _toggle = Controls.Compact("Off", Toggle);
        Axis = Axis.Row;

        var text = Ui.Column(Ui.Slot(Ui.Label(name, 15f)), Ui.Slot(Ui.Label(description, 12f, Ui.Muted)));
        Add(Ui.Slot(text, 1f, CrossFit.Desired, CrossAlign.Center));
        if (control is not null) Add(Ui.Slot(control, fit: CrossFit.Desired, align: CrossAlign.Center));
        Add(Ui.Slot(Ui.Gap(0f, 12f)));
        Add(Controls.Fixed(_toggle, ToggleWidth));
    }

    private void Toggle()
    {
        _slot.Toggle();
        _toggle.SetText(_slot.IsOn ? "On" : "Off");
        Controls.Highlight(_toggle, _slot.IsOn);
    }
}
