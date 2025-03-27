namespace Rin.Engine.Views.Events;

/// <summary>
/// Interfaces for events that will stop propagating once handled
/// </summary>
public interface IHandleableEvent
{
    public bool Handled { get; }
}