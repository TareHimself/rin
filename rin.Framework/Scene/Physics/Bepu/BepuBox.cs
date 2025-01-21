using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Bepu;

public class BepuBox : BepuBody,IPhysicsBox
{
    private Vec3<float> _size;

    public Vec3<float> Size
    {
        get => _size;
        set
        {
            _size = value;
            UpdateShape();
        }
    }

    private Box _shape;
    private readonly TypedIndex _typedIndex;

    public BepuBox(BepuPhysics physics, IPhysicsComponent owner,Vec3<float> size) : base(physics,owner)
    {
        _size = size;
        _shape = CreateBox();
        _typedIndex = physics.Simulation.Shapes.Add(_shape);
    }

    private void UpdateShape()
    {
        _shape = CreateBox();
        Physics.Simulation.Shapes.GetShape<Box>(_typedIndex.Index) = _shape;
    }

    private Box CreateBox()
    {
        return new Box(Size.X, Size.Y, Size.Z);
    }

    protected override TypedIndex GetTypedIndex() => _typedIndex;
    protected override BodyInertia ComputeInertia() => _shape.ComputeInertia(Mass);

    public override void Dispose()
    {
        base.Dispose();
        Physics.Simulation.Shapes.Remove(_typedIndex);
    }
}