using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Examples.TextInput.Views;

/// <summary>
///     A text input that reports every change and exposes the text an input method is composing.
/// </summary>
public sealed class ProbeInputView : TextInputBoxView
{
    public event Action<ProbeInputView>? Changed;

    public required string Label { get; init; }

    public string Placeholder { get; init; } = string.Empty;

    public bool Multiline { get; init; }

    public string Composing { get; private set; } = string.Empty;

    public override void OnCharacter(CharacterSurfaceEvent e)
    {
        if (!Multiline && e.Character is '\n' or '\r') return;
        Composing = string.Empty;
        base.OnCharacter(e);
        Changed?.Invoke(this);
    }

    public override void OnTextPreedit(TextPreeditSurfaceEvent e)
    {
        Composing = e.Text;
        base.OnTextPreedit(e);
        Changed?.Invoke(this);
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        if (!Multiline && e.Key == InputKey.Enter) return;
        base.OnKeyboard(e);
        Changed?.Invoke(this);
    }

    public override void OnCursorDown(CursorDownSurfaceEvent e, in Matrix4x4 transform)
    {
        base.OnCursorDown(e, transform);
        Changed?.Invoke(this);
    }

    public override void OnFocus()
    {
        base.OnFocus();
        Changed?.Invoke(this);
    }

    public override void OnFocusLost()
    {
        Composing = string.Empty;
        base.OnFocusLost();
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);
        if (Content.Length == 0 && Placeholder.Length > 0)
            commands.AddText(transform, FontFamily, Placeholder, FontSize, Ui.Muted);
    }
}
