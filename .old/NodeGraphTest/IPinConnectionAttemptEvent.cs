using Rin.Framework.Views.Events;

namespace NodeGraphTest;

public interface IPinConnectionAttemptEvent : IPositionalEvent, IHandleableEvent
{
    public IPinConnectionRequest Request { get; }

    public IGraphPinView? PinView { get; set; }
}