using System.Numerics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Math;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Math;
using Rin.Engine.World.Views;
using rin.Examples.SceneTest.entities;

namespace rin.Examples.SceneTest;

public class TestViewport(CameraActor camera, TextBox modeText) : Viewport(camera.GetCameraComponent(), modeText)
{
    private readonly Actor _testActor = new Actor
    {
        RootComponent = new StaticMeshComponent
        {
            MeshId = 0,
            Scale = new Vector3(1, 1, 2),
            Location = new Vector3(3, 15, 3),
            Rotation = MathR.Forward.ToQuaternion()
        }
    } ?? throw new NullReferenceException();

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
        _yaw -= delta.X;
        _pitch = float.Clamp(_pitch, -90, 90);
        Console.WriteLine("Pitch: {0}, Yaw: {1}", _pitch, _yaw);
        viewTarget.SetRotation(MathR.Forward.ToQuaternion().AddYaw(_yaw).AddLocalPitch(_pitch));
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _testActor.SetRotation(MathR.LookAt(_testActor.GetLocation(),camera.GetLocation(),MathR.Up).ToQuaternion());
        if (IsFocused)
        {
            var speed = 100.0f;
            var transform = camera.GetTransform(Space.World);
            transform.Location += transform.Rotation.GetForward() * _forwardAxis * deltaTime * speed;
            transform.Location += transform.Rotation.GetRight() * _rightAxis * deltaTime * speed;
            Console.WriteLine("AXIS {0}", _forwardAxis);
            camera.SetTransform(transform, Space.World);
        }
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        base.OnKeyboard(e);
        if (e is { Key: InputKey.Space, State: InputState.Pressed })
        {
            var world = camera.GetTransform(Space.World);
            world.Rotation = MathR.LookAt(world.Location, new Vector3(8, 15, 0), MathR.Up).ToQuaternion();
            camera.SetTransform(world, Space.World);
        }

        if (e is
            {
                Key: InputKey.W or InputKey.A or InputKey.S or InputKey.D,
                State: InputState.Pressed or InputState.Released
            })
        {
            if (e is { Key: InputKey.W })
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => +1.0f,
                    InputState.Released => -1.0f
                };

            if (e is { Key: InputKey.S })
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => -1.0f,
                    InputState.Released => +1.0f
                };

            if (e is { Key: InputKey.D })
                _rightAxis += e.State switch
                {
                    InputState.Pressed => +1.0f,
                    InputState.Released => -1.0f
                };

            if (e is { Key: InputKey.A })
                _rightAxis += e.State switch
                {
                    InputState.Pressed => -1.0f,
                    InputState.Released => +1.0f
                };
        }
    }
}