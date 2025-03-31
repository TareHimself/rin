using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;

namespace Rin.Engine.World;

public static class WorldExtensions
{
    public static Actor CreateMeshEntity(this World world)
    {
        var entity = world.AddActor<Actor>();
        entity.AddComponent<StaticMeshComponent>();
        return entity;
    }
}