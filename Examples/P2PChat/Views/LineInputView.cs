using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace P2PChat.Views;

/// <summary>
///     A single line text input that raises <see cref="Submitted" /> on Enter instead of inserting a new line.
/// </summary>
public sealed class LineInputView : TextInputBoxView
{
    public event Action? Submitted;

    public string Placeholder { get; init; } = string.Empty;

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        if (e is { Key: InputKey.Enter, State: InputState.Pressed })
            Submitted?.Invoke();
        else if (e.Key != InputKey.Enter)
            base.OnKeyboard(e);
    }

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        if (e.Character is not ('\n' or '\r')) base.OnCharacter(e);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);
        if (Content.Length == 0 && Placeholder.Length > 0)
            commands.AddText(transform, FontFamily, Placeholder, FontSize, Ui.Muted);
    }
}
