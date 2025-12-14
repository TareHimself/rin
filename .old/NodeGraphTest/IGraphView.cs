using System.Numerics;
using Rin.Framework.Views.Events;

namespace NodeGraphTest;

public interface IGraphView
{
    public void StartPinDrag(CursorDownSurfaceEvent e,IGraphPinView pin,in Vector2 pinCenter);
    public void StartNodeDragging(CursorDownSurfaceEvent e,IGraphNodeView node);
    
}