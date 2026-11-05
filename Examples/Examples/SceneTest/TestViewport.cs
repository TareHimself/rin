using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Events;
using Rin.World.Ecs;
using Rin.World.Ecs.Graphics;
using Rin.World.Ecs.Transforms;
using Rin.World.Views;

namespace Examples.SceneTest;

public class TestViewport : Viewport
{
    private const float Speed = 50f;

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
        _pitch = float.Clamp(_pitch + delta.Y, -90, 90);
        _yaw += delta.X;
        _world.SetRotation(_camera, MathR.Forward.ToQuaternion().AddYaw(_yaw).AddLocalPitch(_pitch));
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (!IsFocused) return;

        // The camera is a root entity, so its local transform is its world transform.
        ref var transform = ref _world.Get<LocalTransform>(_camera);
        var position = transform.Position;
        position += transform.Rotation.GetForward() * _forwardAxis * deltaTime * Speed;
        position += transform.Rotation.GetRight() * _rightAxis * deltaTime * Speed;
        transform.Position = position;
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        base.OnKeyboard(e);
        if (e is not { State: InputState.Pressed or InputState.Released }) return;

        var sign = e.State == InputState.Pressed ? 1f : -1f;
        switch (e.Key)
        {
            case InputKey.W:
                _forwardAxis += sign;
                break;
            case InputKey.S:
                _forwardAxis -= sign;
                break;
            case InputKey.D:
                _rightAxis += sign;
                break;
            case InputKey.A:
                _rightAxis -= sign;
                break;
        }
    }
}
