using Rin.Core.Views.Graphics;

namespace Rin.Core.Views.Events;

public class TextPreeditSurfaceEvent(ISurface surface, string text, int caretStart, int caretEnd) : SurfaceEvent(surface)
{
    public string Text = text;
    public int CaretStart = caretStart;
    public int CaretEnd = caretEnd;
}
