namespace Rin.Core.Graphics.Windows;

/// <summary>
///     A clipboard that lives in the process, for backends without a system one and for tests.
/// </summary>
public sealed class InMemoryClipboard : IClipboard
{
    private string? _text;

    public bool HasText => _text is not null;

    public string? GetText()
    {
        return _text;
    }

    public bool SetText(string text)
    {
        _text = text;
        return true;
    }

    public void Clear()
    {
        _text = null;
    }
}
