using rin.Framework.Scene.Components;
using rin.Framework.Scene.Entities;

namespace SceneTest.entities;

public class CameraEntity : Entity
{
    private readonly CameraComponent _camera;
    
    public CameraComponent GetCameraComponent() => _camera;
    
    public CameraEntity()
    {
        RootComponent = _camera = AddComponent<CameraComponent>();
    }
}