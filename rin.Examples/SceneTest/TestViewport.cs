using System.Numerics;
using rin.Examples.SceneTest.entities;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics.Windows;
using Rin.Editor.Scene.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Events;

namespace rin.Examples.SceneTest;

public class TestViewport : Viewport
{
    private readonly CameraActor _camera;
    private float _forwardAxis = 0.0f;
    float _rightAxis = 0.0f;
    public TestViewport(CameraActor camera, TextBox modeText) : base(camera.GetCameraComponent(), modeText)
    {
        _camera = camera;
    }

    protected override void OnMouseDelta(Vector2 delta)
    {
        base.OnMouseDelta(delta);
        var viewTarget = _camera?.RootComponent;
        if (viewTarget == null) return;//.ApplyYaw(delta.X).ApplyPitch(delta.Y)
        viewTarget.SetRelativeRotation(viewTarget.GetRelativeRotation().Delta(pitch: delta.Y, yaw: delta.X));
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (IsFocused)
        {
            float speed = 100.0f;
            var transform = _camera.GetWorldTransform();
            transform.Location += transform.Rotation.GetForwardVector() * _forwardAxis * deltaTime * speed;
            transform.Location += transform.Rotation.GetRightVector() * _rightAxis * deltaTime * speed;
            _camera.SetWorldTransform(transform);
        }
    }

    public override void OnKeyboard(KeyboardSurfaceEvent e)
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
}