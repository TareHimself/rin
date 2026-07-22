using Rin.Core;
using Rin.World.Actors;

namespace Rin.World.Components;

public interface IComponent : IUpdatable
{
    public Actor? Owner { get; set; }

    public void Start();

    public void Stop();
}