using Examples.Common;
using System.Numerics;
using Rin.World.Ecs;
using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Transforms;
using Rin.World.Views;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Events;

namespace Examples.Sponza;

public class TestViewport : Viewport
{
    private static readonly Log Log = Log.For<TestViewport>();

    private readonly EntityWorld _world;
    private readonly Entity _camera;

    private float _forwardAxis;
    private float _pitch;
    private float _rightAxis;
    private float _yaw;

    public TestViewport(RenderFeature render, Entity camera) : base(render, camera)
    {
        _world = render.World;
        _camera = camera;
    }

    protected override void OnMouseDelta(Vector2 delta)
    {
        base.OnMouseDelta(delta);
        _pitch += delta.Y;
        _yaw += delta.X;
        _pitch = float.Clamp(_pitch, -90, 90);
        //Console.WriteLine("Pitch: {0}, Yaw: {1}", _pitch, _yaw);
        _world.SetRotation(_camera, MathR.Forward.ToQuaternion().AddYaw(_yaw).AddLocalPitch(_pitch));
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (IsFocused)
        {
            var speed = 100.0f;
            // The camera is a root entity, so its local transform is its world transform.
            ref var transform = ref _world.Get<LocalTransform>(_camera);
            var position = transform.Position;
            position += transform.Rotation.GetForward() * _forwardAxis * deltaTime * speed;
            position += transform.Rotation.GetRight() * _rightAxis * deltaTime * speed;
            transform.Position = position;
        }
    }

    public override void OnCursorUp(CursorUpSurfaceEvent e)
    {
        base.OnCursorUp(e);
        _forwardAxis = 0;
        _rightAxis = 0;
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
            {
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => +moveSpeed,
                    InputState.Released => -moveSpeed
                };
                Log.Info($"Forward {e.State}");
            }

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