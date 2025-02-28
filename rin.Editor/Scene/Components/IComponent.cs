using Rin.Editor.Scene.Actors;
using Rin.Engine.Core;

namespace Rin.Editor.Scene.Components;

public interface IComponent : IReceivesUpdate
{
    public Actor? Owner { get; set; }
    
    public void Start();
    
    public void Stop();
}