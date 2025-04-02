using Rin.Engine.Core;
using Rin.Engine.World.Components;

namespace Rin.Engine.World.Systems;

public interface ISystem : IReceivesUpdate
{
    public bool Tickable { get; }
    public void Startup(World world);
    public void Shutdown(World world);
    public void OnComponentCreated(IComponent component);
    public void OnComponentDestroyed(IComponent component);
}