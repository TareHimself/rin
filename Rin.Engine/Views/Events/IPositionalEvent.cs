using System.Numerics;

namespace Rin.Engine.Views.Events;

/// <summary>
/// Interface for <see cref="ISurfaceEvent"/>'s that require hit testing 
/// </summary>
public interface IPositionalEvent : ISurfaceEvent
{
    
    /// <summary>
    /// The surface position this event occured at
    /// </summary>
    public Vector2 Position { get; }
    
    /// <summary>
    /// If true the children will be hit tested in reverse order useful for stuff like button testing
    /// </summary>
    public bool ReverseTestOrder { get; }
}