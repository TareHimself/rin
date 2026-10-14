namespace Rin.Core.Graphics.Windows;

/// <summary>
///     The system clipboard, text only. A backend without one keeps every call a failing no-op.
/// </summary>
public interface IClipboard
{
    public bool HasText { get; }

    /// <summary>
    ///     The clipboard's text, or null when it holds none.
    /// </summary>
    public string? GetText();

    public bool SetText(string text);

    public void Clear();
}
