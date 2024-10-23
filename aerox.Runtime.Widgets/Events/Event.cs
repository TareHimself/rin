using aerox.Runtime.Widgets.Graphics;

namespace aerox.Runtime.Widgets.Events;

public class Event
{
    public Surface Surface;

    public Event(Surface surface)
    {
        Surface = surface;
    }
}