using Rin.World.Ecs;

namespace Rin.World;

public readonly struct EntityChildren
{
    private readonly ChildStore _hierarchy;
    private readonly int _parent;
    private readonly EntityIdAllocator _ids;

    internal EntityChildren(ChildStore hierarchy, int parent, EntityIdAllocator ids)
    {
        _hierarchy = hierarchy;
        _parent = parent;
        _ids = ids;
    }

    public int Count => _hierarchy.ChildrenOf(_parent).Length;

    public Entity this[int index] => _ids.HandleOf(_hierarchy.ChildrenOf(_parent)[index]);

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator
    {
        private readonly EntityChildren _children;
        private int _index;

        internal Enumerator(EntityChildren children)
        {
            _children = children;
            _index = -1;
        }

        public Entity Current => _children[_index];

        public bool MoveNext() => ++_index < _children.Count;
    }
}
