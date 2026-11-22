using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Examples.Editor.Views;

/// <summary>
///     The dark rounded box every editable value sits in, with an accent border while its input has focus.
/// </summary>
/// <remarks>
///     A click anywhere in the box that no child takes goes to the input, so the whole box is the click target.
/// </remarks>
public sealed class FieldBox : CardView
{
    private readonly TextInputBoxView? _input;

    public FieldBox(IView content, TextInputBoxView? input)
    {
        _input = input;
        Color = EditorTheme.Field;
        BorderRadius = BorderRadius.All(EditorTheme.FieldRadius);
        Padding = new Padding(8f, 5f);
        SetChild(content);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        if (e.Handled) return;
        _input?.OnCursorDown(e, transform);
    }

    protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        BorderColor = _input is { IsFocused: true } ? EditorTheme.Accent : EditorTheme.Border;
        base.CollectSelf(transform, cmds);
    }
}
