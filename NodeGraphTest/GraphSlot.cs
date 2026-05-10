using System.Numerics;
using Rin.Framework.Views.Layouts;

namespace NodeGraphTest;

public class GraphSlot : Slot
{
    public Vector2 Position
    {
        get => Child.Offset;
        set => Child.Offset = value;
    }
}