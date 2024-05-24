using aerox.Runtime.Scene.Entities;

namespace aerox.Runtime.Scene.Components;

public class Component : SceneDisposable
{
    public Entity? Owner;
    
    protected override void OnDispose(bool isManual)
    {
        
    }
}