using Rin.Editor.Scene.Actors;
using Rin.Editor.Scene.Components;

namespace Rin.Editor.Scene;

public static class SceneExtensions
{
    public static Actor CreateMeshEntity(this Scene scene)
    {
        var entity = scene.AddActor<Actor>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}