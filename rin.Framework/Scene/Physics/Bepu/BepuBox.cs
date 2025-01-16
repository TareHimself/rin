using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Bepu;

public class BepuBox : IPhysicsBox
{
    private Vec3<float> _size;
    public ISceneComponent Owner { get; set; }
    
    public Vec3<float> Velocity { get; set; }

    public Vec3<float> Size
    {
        get => _size;
        set
        {
            _size = value;
            UpdateShape();
        }
    }

    public bool IsSimulating { get; private set; } = false;
    public float Mass { get; set; } = 1.0f;
    
    private readonly Box _shape;
    private readonly TypedIndex _typedIndex;
    private readonly BodyHandle _bodyHandle;
    private readonly BepuPhysics _physics;

    public BepuBox(BepuPhysics physics, Vec3<float> size, ISceneComponent owner)
    {
        _physics = physics;
        _size = size;
        Owner = owner;
        _shape = CreateBox();
        _typedIndex = _physics.Simulation.Shapes.Add(_shape);
        _bodyHandle = _physics.Simulation.Bodies.Add(CreateBodyDescription());
    }

    public void SetSimulatePhysics(bool newState)
    {
        var lastState = IsSimulating;
        if (lastState == newState) return;
        UpdateBodyDescription();
        IsSimulating = newState;
    }

    public Transform GetTransform()
    {
        var pose = _physics.Simulation.Bodies[_bodyHandle].Pose;
        return new Transform()
        {
            Location = new Vec3<float>(pose.Position.X, pose.Position.Y, pose.Position.Z),
            Rotation =
                (Rotator)new Quat(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W),
            Scale = Owner.GetSceneTransform().Scale
        };
    }

    public void Dispose()
    {
        _physics.Simulation.Bodies.Remove(_bodyHandle);
        _physics.Simulation.Shapes.Remove(_typedIndex);
    }

    private void UpdateBodyDescription()
    {
        _physics.Simulation.Bodies.ApplyDescription(_bodyHandle, CreateBodyDescription());
    }

    private void UpdateShape()
    {
        _physics.Simulation.Shapes.GetShape<Box>(_typedIndex.Index) = CreateBox();
    }

    private Box CreateBox()
    {
        return new Box(Size.X, Size.Y, Size.Z);
    }

    private BodyDescription CreateBodyDescription()
    {
        var transform = Owner.GetSceneTransform();
        var pose = new RigidPose(transform.Location.ToVector3(), ((Quat)transform.Rotation).ToQuaternion());
        var collidableDescription = new CollidableDescription(_typedIndex);
        var bodyActivityDescription = new BodyActivityDescription(0.01f);
        if (IsSimulating)
        {
            return BodyDescription.CreateDynamic(pose, new BodyVelocity(Velocity.ToVector3()),
                _shape.ComputeInertia(Mass), collidableDescription, bodyActivityDescription);
        }
        else
        {
            return BodyDescription.CreateKinematic(pose, collidableDescription, bodyActivityDescription);
        }
    }
}