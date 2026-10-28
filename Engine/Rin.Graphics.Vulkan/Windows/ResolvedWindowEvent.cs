using System.Numerics;

namespace Rin.Graphics.Vulkan.Windows;

/// <summary>
///     A native window event plus the two things it does not carry itself: the cursor position for cursor events
///     and the text for text events. <see cref="Resolve" /> is the only place that asks native for either, so
///     <see cref="RinWindow.ProcessEvent" /> needs no native calls and a test can build one with a plain string.
/// </summary>
internal readonly record struct ResolvedWindowEvent(
    Native.Window.WindowEvent Raw,
    Vector2 CursorPosition = default,
    string Text = "")
{
    public static ResolvedWindowEvent Resolve(in Native.Window.WindowEvent raw)
    {
        return raw.info.type switch
        {
            Native.Window.EventType.Scroll or Native.Window.EventType.CursorMove
                or Native.Window.EventType.CursorButton =>
                new ResolvedWindowEvent(raw, Native.platformWindowGetCursorPosition(raw.info.windowId)),
            Native.Window.EventType.TextCommit => new ResolvedWindowEvent(raw, Text: ReadText(raw.textCommit.text)),
            Native.Window.EventType.TextPreedit => new ResolvedWindowEvent(raw, Text: ReadText(raw.textPreedit.text)),
            _ => new ResolvedWindowEvent(raw)
        };
    }

    private static unsafe string ReadText(in Native.Window.TextRef text)
    {
        var data = Native.platformWindowGetEventText(text);
        return data == null ? string.Empty : new string(data, 0, (int)text.length);
    }
}
