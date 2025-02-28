using Rin.Engine.Views.Graphics;

namespace Rin.Engine.Views.Events;

public class Event(Surface surface)
{
    public readonly Surface Surface = surface;
    public View? Target { get; set; }
}