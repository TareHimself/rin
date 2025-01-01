using rin.Framework.Scene.Components;
using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene;

public static class SceneExtensions
{
    public static Entity CreateCameraEntity(this Scene scene)
    {
        var entity = scene.CreateEntity();
        entity.AddComponent<CameraComponent>();
        return entity;
    }
    
    public static Entity CreateMeshEntity(this Scene scene)
    {
        var entity = scene.CreateEntity();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}