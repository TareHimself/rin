using Rin.Core.Graphics.Windows;

namespace Rin.Graphics.Vulkan.Windows;

/// <summary>
///     The system clipboard through the native window library.
/// </summary>
internal sealed class SystemClipboard : IClipboard
{
    public bool HasText => Native.platformHasClipboardText() == 1;

    public unsafe string? GetText()
    {
        var text = Native.platformGetClipboardText();
        return text == null ? null : new string(text);
    }

    public unsafe bool SetText(string text)
    {
        fixed (char* chars = text)
        {
            return Native.platformSetClipboardText(chars, (ulong)text.Length) == 1;
        }
    }

    public void Clear()
    {
        Native.platformClearClipboard();
    }
}
