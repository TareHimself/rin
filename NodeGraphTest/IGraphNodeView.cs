using System.Numerics;
using Rin.Framework;
using Rin.Framework.Views;
using Rin.Framework.Views.Events;

namespace NodeGraphTest;

public interface IGraphNodeView : IView, IJsonSerializable
{
    public string InstanceId { get; }
    public IEnumerable<IGraphPinView> InputPins { get; }
    public IEnumerable<IGraphPinView> OutputPins { get; }
    
    public void StartPinDrag(CursorDownSurfaceEvent e,IGraphPinView pin,in Vector2 pinCenter);
    public void TryConnectPin(IPinConnectionRequest request);
}