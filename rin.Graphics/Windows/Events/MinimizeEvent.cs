using rin.Core.Math;
using rin.Windows.Native.src;

namespace rin.Graphics.Windows.Events;

public class MinimizeEvent : Event
{
    public required bool Minimized;
}