﻿using System.Numerics;
using Rin.Engine.Scene.Physics;

namespace Rin.Engine.Scene.Components;

public abstract class CollisionComponent : SceneComponent, IPhysicsComponent
{
    IPhysicsBody? _physicsBody;
    private bool _simulating = false;

    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }
    public float Mass { get; set; } = 1.0f;
    
    public event Action<RayCastResult>? OnHit; 

    public bool IsSimulating
    {
        get
        {
            if (_physicsBody is { } body)
            {
                _simulating = body.IsSimulating;
                return body.IsSimulating;
            }

            return _simulating;
        }
        set
        {
            if (_physicsBody is { } body)
            {
                body.SetSimulatePhysics(value);
                _simulating = body.IsSimulating;
            }
            else
            {
                _simulating = value;
            }
        }
    }

    public void ProcessHit(IPhysicsBody body, RayCastResult result)
    {
        OnHit?.Invoke(result);
    }

    protected abstract IPhysicsBody CreatePhysicsBody();

    public override void Start()
    {
        base.Start();
        _physicsBody = CreatePhysicsBody();
        _physicsBody.SetSimulatePhysics(_simulating);
        _simulating = _physicsBody.IsSimulating;
    }

    public override void Stop()
    {
        base.Stop();
        _physicsBody?.Dispose();
    }
}