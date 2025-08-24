using System.Numerics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Math;
using Rin.Framework.Views.Events;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Math;
using Rin.Engine.World.Views;
using rin.Examples.SceneTest.entities;

namespace rin.Examples.SceneTest;

public class TestViewport(CameraActor camera) : Viewport(camera.GetCameraComponent())
{
    private readonly Actor _testActor = camera;
    // private readonly Actor _testActor = camera.World?.AddActor(new Actor
    // {
    //     RootComponent = new StaticMeshComponent
    //     {
    //         MeshId = 0,
    //         Scale = new Vector3(1, 1, 2),
    //         Location = new Vector3(3, 15, 3),
    //         Rotation = MathR.Forward.ToQuaternion()
    //     }
    // }) ?? throw new NullReferenceException();

    private float _forwardAxis;
    private float _pitch;
    private float _rightAxis;
    private float _yaw;

    protected override void OnMouseDelta(Vector2 delta)
    {
        base.OnMouseDelta(delta);
        var viewTarget = _testActor?.RootComponent;
        if (viewTarget == null) return; //.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        _pitch += delta.Y;
        _yaw += delta.X;
        _pitch = float.Clamp(_pitch, -90, 90);
        Console.WriteLine("Pitch: {0}, Yaw: {1}", _pitch, _yaw);
        viewTarget.SetRotation(MathR.Forward.ToQuaternion().AddYaw(_yaw).AddLocalPitch(_pitch));
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        //_testActor.SetRotation(MathR.LookTowards(_testActor.GetLocation(),camera.GetLocation(),MathR.Up));
        if (IsFocused)
        {
            var speed = 50.0f;
            var transform = camera.GetTransform(Space.World);
            transform.Position += transform.Orientation.GetForward() * _forwardAxis * deltaTime * speed;
            transform.Position += transform.Orientation.GetRight() * _rightAxis * deltaTime * speed;
            Console.WriteLine("AXIS {0}", _forwardAxis);
            camera.SetTransform(transform, Space.World);
        }
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        base.OnKeyboard(e);
        if (e is { Key: InputKey.Space, State: InputState.Pressed })
        {
            // var world = camera.GetTransform(Space.World);
            // world.Rotation = MathR.LookTowards(world.Location, new Vector3(8, 15, 0), MathR.Up);
            // camera.SetTransform(world, Space.World);
        }

        var moveSpeed = 1f;
        if (e is
            {
                Key: InputKey.W or InputKey.A or InputKey.S or InputKey.D,
                State: InputState.Pressed or InputState.Released
            })
        {
            if (e is { Key: InputKey.W })
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => +moveSpeed,
                    InputState.Released => -moveSpeed
                };

            if (e is { Key: InputKey.S })
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => -moveSpeed,
                    InputState.Released => +moveSpeed
                };

            if (e is { Key: InputKey.D })
                _rightAxis += e.State switch
                {
                    InputState.Pressed => +moveSpeed,
                    InputState.Released => -moveSpeed
                };

            if (e is { Key: InputKey.A })
                _rightAxis += e.State switch
                {
                    InputState.Pressed => -moveSpeed,
                    InputState.Released => +moveSpeed
                };
        }
    }
}