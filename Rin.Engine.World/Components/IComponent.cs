using Rin.Engine.Core;
using Rin.Engine.World.Actors;

namespace Rin.Engine.World.Components;

public interface IComponent : IReceivesUpdate
{
    public Actor? Owner { get; set; }

    public void Start();

    public void Stop();
}