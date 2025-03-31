using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Physics.Bepu;

public class BepuBox : BepuBody,IPhysicsBox
{
    private Vector3 _size;

    public Vector3 Size
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

    public BepuBox(BepuPhysics physics, IPhysicsComponent owner,Vector3 size) : base(physics,owner)
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