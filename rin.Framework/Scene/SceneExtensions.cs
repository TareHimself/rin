using rin.Framework.Scene.Actors;
using rin.Framework.Scene.Components;

namespace rin.Framework.Scene;

public static class SceneExtensions
{
    public static Actor CreateMeshEntity(this Scene scene)
    {
        var entity = scene.AddActor<Actor>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}