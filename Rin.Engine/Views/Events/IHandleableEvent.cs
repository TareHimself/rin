namespace Rin.Engine.Views.Events;

public interface IHandleableEvent
{
    /// <summary>
    /// The view that handled this event
    /// </summary>
    public View? Handler { get; set; }
}