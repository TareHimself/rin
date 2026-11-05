using Friflo.Engine.ECS;
using Rin.World.Ecs;

namespace Rin.World;

public readonly struct EntityChildren
{
    private readonly ChildEntities _children;
    private readonly EntityIdAllocator _ids;

    internal EntityChildren(ChildEntities children, EntityIdAllocator ids)
    {
        _children = children;
        _ids = ids;
    }

    public int Count => _children.Count;

    public Entity this[int index] => _ids.HandleOf(_children.Ids[index]);

    public Enumerator GetEnumerator() => new(_children.GetEnumerator(), _ids);

    public struct Enumerator
    {
        private ChildEnumerator _children;
        private readonly EntityIdAllocator _ids;

        internal Enumerator(ChildEnumerator children, EntityIdAllocator ids)
        {
            _children = children;
            _ids = ids;
        }

        public Entity Current => _ids.HandleOf(_children.Current.Id);

        public bool MoveNext() => _children.MoveNext();
    }
}
