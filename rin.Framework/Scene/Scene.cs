using rin.Framework.World.Entities;

namespace rin.Framework.Scene;

public class Scene
{
    private object _lock = new();
    private long _lastEntityId = 0;

    private long MakeEntityId()
    {
        lock (_lock)
        {
            return _lastEntityId++;
        }
    }
    private Dictionary<long,Entity> _entities = [];

    public Entity AddEntity(Entity entity)
    {
        throw new Exception();
    }
}