using BepuPhysics;
using BepuPhysics.Collidables;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene.Physics.Bepu;

public class BepuSphere : IPhysicsSphere
{

    private float _radius;
    public ISceneComponent Owner { get; set; }

    public Vec3<float> Velocity { get; set; }

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            UpdateShape();
        }
    }

    public bool IsSimulating { get; private set; } = false;
    public float Mass { get; set; } = 1.0f;
    private readonly Sphere _shape;
    private readonly TypedIndex _typedIndex;
    private readonly BodyHandle _bodyHandle;
    private readonly BepuPhysics _physics;

    public BepuSphere(BepuPhysics physics, float radius, ISceneComponent owner)
    {
        _physics = physics;
        _radius = radius;
        Owner = owner;
        _shape = CreateShape();
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
        _physics.Simulation.Shapes.GetShape<Sphere>(_typedIndex.Index) = CreateShape();
    }

    private Sphere CreateShape()
    {
        return new Sphere();
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