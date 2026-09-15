using Rin.Core;
using Rin.World.Components;

namespace Rin.World.Systems;

public interface ISystem : IUpdatable
{
    public bool Tickable { get; }
    public void Startup(World world);
    public void Shutdown(World world);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}