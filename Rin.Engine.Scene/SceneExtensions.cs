using Rin.Engine.Scene.Actors;
using Rin.Engine.Scene.Components;

namespace Rin.Engine.Scene;

public static class SceneExtensions
{
    public static Actor CreateMeshEntity(this Scene scene)
    {
        var entity = scene.AddActor<Actor>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}