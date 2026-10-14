namespace Rin.World.Physics;

/// <summary>
///     Receives every hit of an "all" cast as it is found, in traversal order rather than by distance.
/// </summary>
public interface IPhysicsHitHandler
{
    /// <summary>
    ///     Return false to stop the query early.
    /// </summary>
    bool OnHit(in RayCastResult hit);
}
