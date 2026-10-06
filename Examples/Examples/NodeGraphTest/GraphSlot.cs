using System.Numerics;
using Rin.Core.Views.Layouts;

namespace Examples.NodeGraphTest;

public class GraphSlot : Slot
{
    public Vector2 Position
    {
        get => Child.Offset;
        set => Child.Offset = value;
    }
}