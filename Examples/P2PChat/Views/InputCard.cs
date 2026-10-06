using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;

namespace P2PChat.Views;

/// <summary>
///     Wraps a <see cref="LineInputView" /> in a rounded field that gets an accent border while it has focus.
/// </summary>
public sealed class InputCard : CardView
{
    private readonly LineInputView _input;

    public InputCard(LineInputView input, float radius)
    {
        _input = input;
        input.FontSize = 18f;
        Color = Ui.Raised;
        BorderRadius = new Vector4(radius);
        Padding = new Padding(16f, 11f);
        SetChild(input);
    }

    protected override void CollectSelf(Matrix4x4 transform, CommandList cmds)
    {
        BorderColor = _input.IsFocused ? Ui.Accent : Ui.Border;
        base.CollectSelf(transform, cmds);
    }
}
