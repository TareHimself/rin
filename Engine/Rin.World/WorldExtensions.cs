using Rin.World.Actors;
using Rin.World.Components;

namespace Rin.World;

public static class WorldExtensions
{
    public static Actor CreateMeshEntity(this World world)
    {
        var entity = world.AddActor<Actor>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}