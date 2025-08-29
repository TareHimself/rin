namespace Rin.Framework.Views.Events;

/// <summary>
/// For events that have some kind of target
/// </summary>
public interface ITargetedEvent : ISurfaceEvent
{
    public IView? Target { get; }
}