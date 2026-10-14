namespace Rin.World.Physics;

/// <summary>
///     Receives every body an overlap query finds, in no particular order.
/// </summary>
public interface IPhysicsOverlapHandler
{
    /// <summary>
    ///     Return false to stop the query early.
    /// </summary>
    bool OnOverlap(PhysicsBodyHandle body);
}
