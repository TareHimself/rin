using System.Numerics;

namespace Rin.World.Physics;

/// <summary>
///     The body's linear and angular velocity. Setters mark it dirty so the next tick pushes it to the body; simulated
///     bodies write the stepped velocity back without marking it dirty.
/// </summary>
public struct Velocity() : IComponentData
{
    private Vector3 _linear;
    private Vector3 _angular;

    public Vector3 Linear
    {
        readonly get => _linear;
        set
        {
            _linear = value;
            IsDirty = true;
        }
    }

    public Vector3 Angular
    {
        readonly get => _angular;
        set
        {
            _angular = value;
            IsDirty = true;
        }
    }

    /// <summary>
    ///     Set by every setter and on creation, cleared when the velocity is pushed to the body.
    /// </summary>
    public bool IsDirty { get; internal set; } = true;

    internal void PullFromBody(in Vector3 linear, in Vector3 angular)
    {
        _linear = linear;
        _angular = angular;
    }
}
