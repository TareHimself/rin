using System.Numerics;

namespace Rin.World.Ecs.Transforms;

/// <summary>
///     Position, rotation and scale relative to the parent entity, or to the world for a root.
/// </summary>
/// <remarks>
///     Every setter marks the transform dirty, so writes through a query span are picked up by
///     <see cref="TransformPropagation" /> without a structural change.
/// </remarks>
public struct LocalTransform() : IComponentData
{
    public Vector3 Position
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = Vector3.Zero;

    public Quaternion Rotation
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = Quaternion.Identity;

    public Vector3 Scale
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    } = Vector3.One;

    /// <summary>
    ///     Set by every setter and on creation, cleared by <see cref="TransformPropagation" />.
    /// </summary>
    public bool IsDirty { get; internal set; } = true;

    public readonly Matrix4x4 ToMatrix() =>
        Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
}
