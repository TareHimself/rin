using Rin.World;

namespace Examples.Editor;

public sealed class EditorSelection
{
    public Entity Entity { get; private set; }

    public event Action? Changed;

    public void Select(Entity entity)
    {
        if (entity == Entity) return;

        Entity = entity;
        Changed?.Invoke();
    }
}
