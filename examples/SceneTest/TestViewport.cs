using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.Windows;
using rin.Framework.Scene.Views;
using rin.Framework.Views.Content;
using rin.Framework.Views.Events;
using SceneTest.entities;

namespace SceneTest;

public class TestViewport : Viewport
{
    private readonly CameraActor _camera;
    private float _forwardAxis = 0.0f;
    float _rightAxis = 0.0f;
    public TestViewport(CameraActor camera, TextBox modeText) : base(camera.GetCameraComponent(), modeText)
    {
        _camera = camera;
    }

    protected override void OnMouseDelta(Vec2<float> delta)
    {
        base.OnMouseDelta(delta);
        var viewTarget = _camera?.RootComponent;
        if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }

    public override void OnFocus()
    {
        base.OnFocus();
        SRuntime.Get().OnUpdate += OnUpdate;
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        SRuntime.Get().OnUpdate -= OnUpdate;
    }

    public override void OnKeyboard(KeyboardEvent e)
    {
        base.OnKeyboard(e);
        if (e is { Key: InputKey.W or InputKey.A or InputKey.S or InputKey.D, State: InputState.Pressed or InputState.Released })
        {
            if (e is { Key: InputKey.W  })
            {
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => +1.0f,
                    InputState.Released => -1.0f,
                };
            }
            
            if (e is { Key: InputKey.S })
            {
                _forwardAxis += e.State switch
                {
                    InputState.Pressed => -1.0f,
                    InputState.Released => +1.0f,
                };
            }
            
            if (e is { Key: InputKey.D })
            {
                _rightAxis += e.State switch
                {
                    InputState.Pressed => +1.0f,
                    InputState.Released => -1.0f,
                };
            }
            
            if (e is { Key: InputKey.A })
            {
                _rightAxis += e.State switch
                {
                    InputState.Pressed => -1.0f,
                    InputState.Released => +1.0f,
                };
            }
        }
    }

    void OnUpdate(double delta)
    {
        float speed = 100.0f;
        var transform = _camera.GetWorldTransform();
        transform.Location += transform.Rotation.GetForwardVector() * _forwardAxis * (float)delta * speed;
        transform.Location += transform.Rotation.GetRightVector() * _rightAxis * (float)delta * speed;
        _camera.SetWorldTransform(transform);
    }
}