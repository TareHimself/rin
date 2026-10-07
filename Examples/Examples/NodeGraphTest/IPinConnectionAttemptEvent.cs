using Rin.Core.Views.Events;

namespace Examples.NodeGraphTest;

public interface IPinConnectionAttemptEvent : IPositionalEvent, IHandleableEvent
{
    public IPinConnectionRequest Request { get; }

    public IGraphPinView? PinView { get; set; }
}