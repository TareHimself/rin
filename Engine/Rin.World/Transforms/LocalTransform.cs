using System.Numerics;

namespace Rin.World.Transforms;

/// <summary>
///     Position, rotation and scale relative to the parent entity, or to the world for a root.
/// </summary>
/// <remarks>
///     Write it through <see cref="World.Write{T}" /> or a query chunk, so <see cref="TransformPropagation" /> sees the
///     change tick.
/// </remarks>
public struct LocalTransform() : IComponentData
{
    public Vector3 Position = Vector3.Zero;

    public Quaternion Rotation = Quaternion.Identity;

    public Vector3 Scale = Vector3.One;

    public readonly Matrix4x4 ToMatrix() =>
        Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
}
