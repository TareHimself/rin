namespace Rin.Core.Graphics.Windows.Events;

/// <summary>
///     Text an input method is still composing. Each event replaces the last one, and empty text ends it.
/// </summary>
public class TextPreeditEvent : WindowEvent
{
    public required string Text;
    public required int CaretStart;
    public required int CaretEnd;
}
