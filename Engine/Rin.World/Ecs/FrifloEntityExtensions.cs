using FrifloEntity = Friflo.Engine.ECS.Entity;

namespace Rin.World.Ecs;

internal static class FrifloEntityExtensions
{
    public static Entity ToHandle(this FrifloEntity entity) => entity.IsNull ? default : new Entity(entity.Id, entity.Revision);
}
