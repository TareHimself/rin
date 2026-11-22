using Rin.Core.Views.Content;

namespace Examples.Editor.Views;

/// <summary>
///     A text input that reports every edit, for filtering a list as the user types.
/// </summary>
public sealed class SearchInputView : TextInputBoxView
{
    public event Action<string>? Edited;

    protected override void TextChanged(string newText)
    {
        base.TextChanged(newText);
        Edited?.Invoke(newText);
    }
}
