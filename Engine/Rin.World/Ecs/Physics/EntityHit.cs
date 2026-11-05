using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     A cast hit plus the entity that owns the hit body, or <c>default</c> when the body has no entity.
/// </summary>
public readonly record struct EntityHit(Entity Entity, RayCastResult Hit);
