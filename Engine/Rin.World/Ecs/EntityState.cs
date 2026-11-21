namespace Rin.World.Ecs;

/// <summary>
///     Where an entity id slot is in its lifecycle.
/// </summary>
internal enum EntityState : byte
{
    Free,
    Alive,

    /// <summary>
    ///     Collected by a despawn whose remove hooks have not finished. Still alive, but it cannot be despawned again or
    ///     reparented.
    /// </summary>
    Despawning
}
