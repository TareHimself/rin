using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Core.Shared.Math;

namespace Rin.World.Physics.Bepu;

/// <summary>
///     Several primitives moving as one body. Bepu centres the compound on its centre of mass, which
///     <see cref="BepuBody.LocalCenter" /> keeps hidden, and the entity's scale is baked into the parts at creation.
/// </summary>
internal sealed class BepuCompoundBody : BepuBody
{
    private readonly TypedIndex _shapeIndex;
    private readonly Vector3 _appliedScale;
    private readonly Vector3 _localCenter;
    private readonly BodyInertia _unitInertia;

    public BepuCompoundBody(PhysicsState state, Transform transform, BepuPhysicsSystem system,
        ReadOnlySpan<ShapePart> parts) : base(state, transform, system)
    {
        if (parts.IsEmpty) throw new ArgumentException("A compound needs at least one part.", nameof(parts));

        _appliedScale = transform.Scale;
        var shapes = System.Simulation.Shapes;
        var totalVolume = 0f;
        foreach (var part in parts) totalVolume += VolumeOf(part.Shape, _appliedScale);

        // CompoundBuilder is a struct, so it is passed by ref and disposed by hand rather than with using.
        var builder = new CompoundBuilder(System.Simulation.BufferPool, shapes, parts.Length);
        try
        {
            foreach (var part in parts)
                AddPart(ref builder, part, _appliedScale, VolumeOf(part.Shape, _appliedScale) / totalVolume);
            builder.BuildDynamicCompound(out var children, out _unitInertia, out _localCenter);
            _shapeIndex = shapes.Add(new Compound(children));
        }
        finally
        {
            builder.Dispose();
        }
    }

    protected override Vector3 LocalCenter => _localCenter;

    protected override TypedIndex GetShapeIndex()
    {
        return _shapeIndex;
    }

    protected override void DisposeShape()
    {
        System.Simulation.Shapes.RecursivelyRemoveAndDispose(_shapeIndex, System.Simulation.BufferPool);
    }

    protected override void UpdateShape()
    {
        UpdateInertia();
    }

    protected override void ScaleUpdated()
    {
        if (GetScale() != _appliedScale)
            throw new NotSupportedException("A compound's scale is fixed at creation");
    }

    // Inertia grows linearly with mass, so the unit-mass inertia from the builder only needs dividing.
    protected override BodyInertia ComputeInertia()
    {
        var mass = GetMass();
        return new BodyInertia
        {
            InverseMass = _unitInertia.InverseMass / mass,
            InverseInertiaTensor = Scale(_unitInertia.InverseInertiaTensor, 1f / mass)
        };
    }

    private static void AddPart(ref CompoundBuilder builder, in ShapePart part, Vector3 scale, float weight)
    {
        var pose = new RigidPose(part.Position * scale, part.Rotation);
        var shape = part.Shape;
        switch (shape.Kind)
        {
            case ColliderShapeKind.Box:
                var size = shape.Size * scale;
                builder.Add(new Box(size.X, size.Y, size.Z), pose, weight);
                break;
            case ColliderShapeKind.Sphere:
                builder.Add(new Sphere(shape.Radius * MaxOf(scale)), pose, weight);
                break;
            case ColliderShapeKind.Capsule:
                builder.Add(new Capsule(shape.Radius * float.Max(scale.X, scale.Z), shape.HalfHeight * 2f * scale.Y),
                    pose, weight);
                break;
            default:
                throw new ArgumentException("A compound's parts must be boxes, spheres or capsules.", nameof(part));
        }
    }

    private static float VolumeOf(in ColliderShape shape, Vector3 scale) => shape.Kind switch
    {
        ColliderShapeKind.Box => float.Max(shape.Size.X * shape.Size.Y * shape.Size.Z * scale.X * scale.Y * scale.Z, 1e-6f),
        ColliderShapeKind.Sphere => 4f / 3f * MathF.PI * MathF.Pow(shape.Radius * MaxOf(scale), 3f),
        ColliderShapeKind.Capsule => CapsuleVolume(shape.Radius * float.Max(scale.X, scale.Z), shape.HalfHeight * scale.Y),
        _ => throw new ArgumentException("A compound's parts must be boxes, spheres or capsules.", nameof(shape))
    };

    private static float CapsuleVolume(float radius, float halfHeight) =>
        MathF.PI * radius * radius * (4f / 3f * radius + 2f * halfHeight);

    private static float MaxOf(Vector3 scale) => float.Max(scale.X, float.Max(scale.Y, scale.Z));

    private static BepuUtilities.Symmetric3x3 Scale(in BepuUtilities.Symmetric3x3 tensor, float factor) => new()
    {
        XX = tensor.XX * factor,
        YX = tensor.YX * factor,
        YY = tensor.YY * factor,
        ZX = tensor.ZX * factor,
        ZY = tensor.ZY * factor,
        ZZ = tensor.ZZ * factor
    };
}
