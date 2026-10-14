using System.Numerics;

namespace Rin.World.Physics;

/// <summary>
///     Buffer-filling forms of the "all" casts and the overlaps. Each fills the caller's span and returns how many
///     entries were written. A full buffer means the query stopped early, not that there were exactly that many.
/// </summary>
public static class PhysicsQueryExtensions
{
    public static unsafe int RayCastAll(this IPhysicsSystem physics, in Vector3 begin, in Vector3 direction, float distance,
        Span<RayCastResult> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (RayCastResult* buffer = hits)
        {
            var collector = new SpanHitCollector(buffer, hits.Length);
            physics.RayCastAll(begin, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int SphereCastAll(this IPhysicsSystem physics, float radius, in Vector3 begin, in Vector3 direction,
        float distance, Span<RayCastResult> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (RayCastResult* buffer = hits)
        {
            var collector = new SpanHitCollector(buffer, hits.Length);
            physics.SphereCastAll(radius, begin, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int BoxCastAll(this IPhysicsSystem physics, in Vector3 size, in Vector3 begin, in Quaternion orientation,
        in Vector3 direction, float distance, Span<RayCastResult> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (RayCastResult* buffer = hits)
        {
            var collector = new SpanHitCollector(buffer, hits.Length);
            physics.BoxCastAll(size, begin, orientation, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int CapsuleCastAll(this IPhysicsSystem physics, float radius, float halfHeight, in Vector3 begin,
        in Quaternion orientation, in Vector3 direction, float distance, Span<RayCastResult> hits, int channel = -1)
    {
        if (hits.IsEmpty) return 0;
        fixed (RayCastResult* buffer = hits)
        {
            var collector = new SpanHitCollector(buffer, hits.Length);
            physics.CapsuleCastAll(radius, halfHeight, begin, orientation, direction, distance, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int OverlapSphere(this IPhysicsSystem physics, float radius, in Vector3 center,
        Span<PhysicsBodyHandle> bodies, int channel = -1)
    {
        if (bodies.IsEmpty) return 0;
        fixed (PhysicsBodyHandle* buffer = bodies)
        {
            var collector = new SpanOverlapCollector(buffer, bodies.Length);
            physics.OverlapSphere(radius, center, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int OverlapBox(this IPhysicsSystem physics, in Vector3 size, in Vector3 center, in Quaternion orientation,
        Span<PhysicsBodyHandle> bodies, int channel = -1)
    {
        if (bodies.IsEmpty) return 0;
        fixed (PhysicsBodyHandle* buffer = bodies)
        {
            var collector = new SpanOverlapCollector(buffer, bodies.Length);
            physics.OverlapBox(size, center, orientation, ref collector, channel);
            return collector.Count;
        }
    }

    public static unsafe int OverlapCapsule(this IPhysicsSystem physics, float radius, float halfHeight, in Vector3 center,
        in Quaternion orientation, Span<PhysicsBodyHandle> bodies, int channel = -1)
    {
        if (bodies.IsEmpty) return 0;
        fixed (PhysicsBodyHandle* buffer = bodies)
        {
            var collector = new SpanOverlapCollector(buffer, bodies.Length);
            physics.OverlapCapsule(radius, halfHeight, center, orientation, ref collector, channel);
            return collector.Count;
        }
    }

    public static void SortByDistance(this Span<RayCastResult> hits)
    {
        hits.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
    }
}
