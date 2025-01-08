using rin.Framework.Scene.Components;
using rin.Framework.Scene.Entities;

namespace rin.Framework.Scene;

public static class SceneExtensions
{
    public static Entity CreateMeshEntity(this Scene scene)
    {
        var entity = scene.AddEntity<Entity>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}