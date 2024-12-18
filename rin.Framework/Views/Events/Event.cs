using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class Event(Surface surface)
{
    public readonly Surface Surface = surface;
    public View? Target { get; set; }
}