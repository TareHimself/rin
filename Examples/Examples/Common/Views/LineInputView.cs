using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Examples.Common.Views;

/// <summary>
///     A single line text input that raises <see cref="Submitted" /> on Enter instead of inserting a new line, and
///     <see cref="Changed" /> after every edit.
/// </summary>
public sealed class LineInputView : TextInputBoxView
{
    public event Action? Submitted;

    public event Action? Changed;

    public string Placeholder { get; init; } = string.Empty;

    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        Changed?.Invoke();
    }

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

    public override void InsertText(string text)
    {
        base.InsertText(text.Replace("\r", string.Empty).Replace("\n", " "));
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);
        if (Content.Length == 0 && Placeholder.Length > 0)
            commands.AddText(transform, FontFamily, Placeholder, FontSize, Ui.Muted);
    }
}
