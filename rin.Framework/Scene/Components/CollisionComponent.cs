using rin.Framework.Scene.Physics;

namespace rin.Framework.Scene.Components;

public abstract class CollisionComponent : SceneComponent
{
    IPhysicsBody? _physicsBody;
    private bool _simulating = false;

    public bool IsSimulating
    {
        get => _simulating;
        set
        {
            _simulating = value;
            _physicsBody?.SetSimulatePhysics(_simulating);
        }
    }

    protected abstract IPhysicsBody CreatePhysicsBody();

    public override void Start()
    {
        base.Start();
        _physicsBody = CreatePhysicsBody();
        _physicsBody.SetSimulatePhysics(_simulating);
    }

    public override void Stop()
    {
        base.Stop();
        _physicsBody?.Dispose();
    }
}