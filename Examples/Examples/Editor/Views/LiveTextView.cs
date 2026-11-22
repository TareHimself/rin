using Rin.Core.Views.Content;

namespace Examples.Editor.Views;

/// <summary>
///     Text for a value read every frame, formatted only when the value changes.
/// </summary>
public sealed class LiveTextView<T>(Func<T> getValue, Func<T, string> format) : TextBoxView where T : IEquatable<T>
{
    private T? _shown;
    private bool _hasValue;

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var value = getValue();
        if (_hasValue && value.Equals(_shown)) return;

        _shown = value;
        _hasValue = true;
        Content = format(value);
    }
}
