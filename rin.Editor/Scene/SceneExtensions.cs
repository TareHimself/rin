using rin.Editor.Scene.Actors;
using rin.Editor.Scene.Components;

namespace rin.Editor.Scene;

public static class SceneExtensions
{
    public static Actor CreateMeshEntity(this Scene scene)
    {
        var entity = scene.AddActor<Actor>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}