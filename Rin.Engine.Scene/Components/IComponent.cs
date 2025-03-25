using Rin.Engine.Core;
using Rin.Engine.Scene.Actors;

namespace Rin.Engine.Scene.Components;

public interface IComponent : IReceivesUpdate
{
    public Actor? Owner { get; set; }
    
    public void Start();
    
    public void Stop();
}