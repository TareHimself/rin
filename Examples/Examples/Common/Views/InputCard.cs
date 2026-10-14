using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Examples.Common.Views;

/// <summary>
///     Wraps a text input in a rounded field that gets an accent border while it has focus.
/// </summary>
public sealed class InputCard : CardView
{
    private readonly TextInputBoxView _input;

    public InputCard(TextInputBoxView input, float radius)
    {
        _input = input;
        input.FontSize = 18f;
        Color = Ui.Raised;
        BorderRadius = BorderRadius.All(radius);
        Padding = new Padding(16f, 11f);
        SetChild(input);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        _input.OnCursorDown(e, transform);
    }

    protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        BorderColor = _input.IsFocused ? Ui.Accent : Ui.Border;
        base.CollectSelf(transform, cmds);
    }
}
