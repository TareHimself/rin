using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.Editor.Views;

/// <summary>
///     A field showing a live value that opens its choices as a list right under it, inside its parent, so the list
///     pushes the rows below it down instead of floating over them.
/// </summary>
public sealed class DropdownView<T> : ListView where T : IEquatable<T>
{
    private readonly IReadOnlyList<T> _choices;
    private readonly Func<T, string> _format;
    private readonly Action<T>? _choose;
    private readonly ListSlot _listSlot;

    public DropdownView(Func<T> getValue, IReadOnlyList<T> choices, Func<T, string> format, Action<T>? choose)
        : base(Axis.Column)
    {
        _choices = choices;
        _format = format;
        _choose = choose;
        _listSlot = new ListSlot { Child = ChoiceList(), Fit = CrossFit.Fill };
        Add(new ListSlot { Child = Header(getValue), Fit = CrossFit.Fill });
    }

    private bool IsOpen => GetSlots().Contains(_listSlot);

    private IView Header(Func<T> getValue)
    {
        var value = new LiveTextView<T>(getValue, _format)
        {
            FontSize = EditorTheme.FontSize,
            ForegroundColor = _choose is null ? EditorTheme.Muted : EditorTheme.Text
        };
        var content = Ui.Row(Ui.Slot(value, 1f, align: CrossAlign.Center));
        if (_choose is not null)
        {
            var chevron = new FontIconView { FontFamily = IconFont.Family, Icon = EditorIcons.Expanded, IconSize = 9f, Color = EditorTheme.Faint };
            content.Add(Ui.Slot(chevron, fit: CrossFit.Desired, align: CrossAlign.Center));
        }

        var box = new FieldBox(content, null);
        if (_choose is null) return box;

        var button = new ButtonView { Color = Ui.Clear };
        button.SetChild(box);
        button.OnReleased += (_, _) => Toggle();
        return button;
    }

    private IView ChoiceList()
    {
        var list = new ListView(Axis.Column);
        foreach (var choice in _choices)
        {
            var row = new ToolButton(null, _format(choice), () => Choose(choice), ToolButtonStyle.Quiet)
            {
                IsFullWidth = true
            };
            list.Add(new ListSlot { Child = row, Fit = CrossFit.Fill });
        }

        return new CardView
        {
            Color = EditorTheme.Card,
            BorderColor = EditorTheme.BorderStrong,
            BorderRadius = BorderRadius.All(EditorTheme.FieldRadius),
            Padding = new Padding(4f),
            InitChild = list
        };
    }

    private void Choose(T choice)
    {
        _choose?.Invoke(choice);
        Toggle();
    }

    private void Toggle()
    {
        if (IsOpen) Remove(_listSlot.Child);
        else Add(_listSlot);
    }
}
