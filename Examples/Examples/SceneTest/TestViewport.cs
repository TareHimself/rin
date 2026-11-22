using System.Numerics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Events;
using Rin.World;
using Rin.World.Graphics;
using Rin.World.Transforms;
using Rin.World.Views;

namespace Examples.SceneTest;

public class TestViewport : Viewport
{
    private const float DefaultSpeed = 50f;

    private readonly World _world;
    private readonly Entity _camera;
    private readonly float _speed;

    private bool _forwardHeld;
    private bool _backHeld;
    private bool _rightHeld;
    private bool _leftHeld;
    private float _pitch;
    private float _yaw;

    // Pass the camera's starting pitch and yaw in degrees so the first mouse drag continues from them.
    public TestViewport(RenderFeature render, Entity camera, float pitch = 0f, float yaw = 0f,
        float speed = DefaultSpeed) : base(render, camera)
    {
        _world = render.World;
        _camera = camera;
        _pitch = pitch;
        _yaw = yaw;
        _speed = speed;
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
        ref var transform = ref _world.Write<LocalTransform>(_camera);
        var position = transform.Position;
        position += transform.Rotation.GetForward() * Axis(_forwardHeld, _backHeld) * deltaTime * _speed;
        position += transform.Rotation.GetRight() * Axis(_rightHeld, _leftHeld) * deltaTime * _speed;
        transform.Position = position;
    }

    private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

    public override void OnKeyboard(KeyboardSurfaceEvent e)
    {
        base.OnKeyboard(e);
        if (e is not { State: InputState.Pressed or InputState.Released }) return;

        var isHeld = e.State == InputState.Pressed;
        switch (e.Key)
        {
            case InputKey.W:
                _forwardHeld = isHeld;
                break;
            case InputKey.S:
                _backHeld = isHeld;
                break;
            case InputKey.D:
                _rightHeld = isHeld;
                break;
            case InputKey.A:
                _leftHeld = isHeld;
                break;
        }
    }

    // Key releases after focus leaves go elsewhere, so nothing may stay held past it.
    public override void OnFocusLost()
    {
        base.OnFocusLost();
        _forwardHeld = _backHeld = _rightHeld = _leftHeld = false;
    }
}
