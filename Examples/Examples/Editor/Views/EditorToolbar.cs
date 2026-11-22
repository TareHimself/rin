using Examples.Common.Views;
using Examples.Editor.Model;
using Rin.Core;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;

namespace Examples.Editor.Views;

/// <summary>
///     The bar across the top of the editor: Play or Stop, Pause or Resume, and Step, centered.
/// </summary>
/// <remarks>
///     Commands run on the main dispatcher, since Play and Stop restore a snapshot, which cannot happen mid-update.
/// </remarks>
public sealed class EditorToolbar : BarView
{
    private readonly PlaySession _session;
    private readonly ToolButton _play;
    private readonly ToolButton _pause;
    private readonly ToolButton _step;

    public EditorToolbar(PlaySession session, string sceneName)
    {
        _session = session;
        _play = new ToolButton(EditorIcons.Play, "Play",
            () => Run(_session.State == PlayState.Editing ? PlayCommand.Play : PlayCommand.Stop), ToolButtonStyle.Primary);
        _pause = new ToolButton(EditorIcons.Pause, null, () => Run(_session.State == PlayState.Paused ? PlayCommand.Resume : PlayCommand.Pause));
        _step = new ToolButton(EditorIcons.Step, null, () => Run(PlayCommand.Step));

        Color = EditorTheme.Panel;
        Padding = new Padding(14f, 8f);
        SetChild(Ui.Row(
            Ui.Slot(Ui.Label("Rin editor", 14f, EditorTheme.Text), fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 12f), fit: CrossFit.Desired),
            Ui.Slot(Ui.Label(sceneName, EditorTheme.FontSize, EditorTheme.Muted), fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f), 1f),
            Ui.Slot(_play, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 6f), fit: CrossFit.Desired),
            Ui.Slot(_pause, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f, 6f), fit: CrossFit.Desired),
            Ui.Slot(_step, fit: CrossFit.Desired, align: CrossAlign.Center),
            Ui.Slot(Ui.Gap(0f), 1f)));

        session.Changed += Refresh;
        Refresh();
    }

    private void Run(PlayCommand command) =>
        IApplication.Get().MainDispatcher.Enqueue(() =>
        {
            if (_session.CanApply(command)) _session.Apply(command);
        });

    private void Refresh()
    {
        var state = _session.State;
        _play.SetIcon(state == PlayState.Editing ? EditorIcons.Play : EditorIcons.Stop);
        _play.SetText(state == PlayState.Editing ? "Play" : "Stop");
        _pause.SetEnabled(state != PlayState.Editing);
        _pause.SetActive(state == PlayState.Paused);
        _step.SetEnabled(_session.CanApply(PlayCommand.Step));
    }
}
