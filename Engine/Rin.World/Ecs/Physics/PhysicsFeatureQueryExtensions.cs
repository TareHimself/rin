using System.Numerics;
using Rin.World.Physics;

namespace Rin.World.Ecs.Physics;

/// <summary>
///     The <see cref="IPhysicsSystem" /> queries with hits mapped to entities. The "all" forms and the overlaps fill the
///     caller's span and return how many entries were written; a full buffer means the query stopped early. Overlaps skip
///     bodies without an entity.
/// </summary>
public static class PhysicsFeatureQueryExtensions
{
    public static EntityHit? RayCast(this PhysicsFeature physics, in Vector3 begin, in Vector3 direction, float distance,
        int channel = -1) =>
        ToEntityHit(physics, physics.PhysicsSystem.RayCast(begin, direction, distance, channel));

    public static EntityHit? SphereCast(this PhysicsFeature physics, float radius, in Vector3 begin, in Vector3 direction,
        float distance, int channel = -1) =>
        ToEntityHit(physics, physics.PhysicsSystem.SphereCast(radius, begin, direction, distance, channel));

    public static EntityHit? BoxCast(this PhysicsFeature physics, in Vector3 size, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, int channel = -1) =>
        ToEntityHit(physics, physics.PhysicsSystem.BoxCast(size, begin, orientation, direction, distance, channel));

    public static EntityHit? CapsuleCast(this PhysicsFeature physics, float radius, float halfHeight, in Vector3 begin,
        in Quaternion orientation, in Vector3 direction, float distance, int channel = -1) =>
        ToEntityHit(physics, physics.PhysicsSystem.CapsuleCast(radius, halfHeight, begin, orientation, direction, distance, channel));

    public static unsafe int RayCastAll(this PhysicsFeature physics, in Vector3 begin, in Vector3 direction, float distance,
        Span<EntityHit> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (EntityHit* buffer = hits)
        {
            var collector = new EntityHitCollector(buffer, hits.Length, physics.Bodies);
            physics.PhysicsSystem.RayCastAll(begin, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int SphereCastAll(this PhysicsFeature physics, float radius, in Vector3 begin, in Vector3 direction,
        float distance, Span<EntityHit> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (EntityHit* buffer = hits)
        {
            var collector = new EntityHitCollector(buffer, hits.Length, physics.Bodies);
            physics.PhysicsSystem.SphereCastAll(radius, begin, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int BoxCastAll(this PhysicsFeature physics, in Vector3 size, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, Span<EntityHit> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (EntityHit* buffer = hits)
        {
            var collector = new EntityHitCollector(buffer, hits.Length, physics.Bodies);
            physics.PhysicsSystem.BoxCastAll(size, begin, orientation, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int CapsuleCastAll(this PhysicsFeature physics, float radius, float halfHeight, in Vector3 begin,
        in Quaternion orientation, in Vector3 direction, float distance, Span<EntityHit> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (EntityHit* buffer = hits)
        {
            var collector = new EntityHitCollector(buffer, hits.Length, physics.Bodies);
            physics.PhysicsSystem.CapsuleCastAll(radius, halfHeight, begin, orientation, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int OverlapSphere(this PhysicsFeature physics, float radius, in Vector3 center, Span<Entity> entities,
        int channel = -1)
    {
        if (entities.IsEmpty) return 0;
        fixed (Entity* buffer = entities)
        {
            var collector = new EntityOverlapCollector(buffer, entities.Length, physics.Bodies);
            physics.PhysicsSystem.OverlapSphere(radius, center, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int OverlapBox(this PhysicsFeature physics, in Vector3 size, in Vector3 center, in Quaternion orientation,
        Span<Entity> entities, int channel = -1)
    {
        if (entities.IsEmpty) return 0;
        fixed (Entity* buffer = entities)
        {
            var collector = new EntityOverlapCollector(buffer, entities.Length, physics.Bodies);
            physics.PhysicsSystem.OverlapBox(size, center, orientation, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int OverlapCapsule(this PhysicsFeature physics, float radius, float halfHeight, in Vector3 center,
        in Quaternion orientation, Span<Entity> entities, int channel = -1)
    {
        if (entities.IsEmpty) return 0;
        fixed (Entity* buffer = entities)
        {
            var collector = new EntityOverlapCollector(buffer, entities.Length, physics.Bodies);
            physics.PhysicsSystem.OverlapCapsule(radius, halfHeight, center, orientation, ref collector, channel);
            return collector.Count;
        }
    }

    private static EntityHit? ToEntityHit(PhysicsFeature physics, RayCastResult? hit) =>
        hit is { } result ? new EntityHit(physics.FindEntity(result.Body), result) : null;
}
