using rin.Framework.Core.Math;
using rin.Framework.Views.Graphics;

namespace rin.Framework.Views.Events;

public class ScrollEvent(Surface surface, Vec2<float> position, Vec2<float> delta)
    : Event(surface)
{
    public Vec2<float> Delta = delta;
    public Vec2<float> Position = position;
}