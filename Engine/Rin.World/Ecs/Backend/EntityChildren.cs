using Friflo.Engine.ECS;
using Rin.World.Ecs.Backend;

namespace Rin.World.Ecs;

public readonly struct EntityChildren
{
    private readonly ChildEntities _children;

    internal EntityChildren(ChildEntities children) => _children = children;

    public int Count => _children.Count;

    public Entity this[int index] => _children[index].ToHandle();

    public Enumerator GetEnumerator() => new(_children.GetEnumerator());

    public struct Enumerator
    {
        private ChildEnumerator _children;

        internal Enumerator(ChildEnumerator children) => _children = children;

        public Entity Current => _children.Current.ToHandle();

        public bool MoveNext() => _children.MoveNext();
    }
}
