using rin.Core.Math;
using rin.Windows.Native.src;

namespace rin.Graphics.Windows.Events;

public class DropEvent : Event
{
    public required string[] Paths;
}