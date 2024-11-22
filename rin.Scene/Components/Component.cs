using rin.Scene.Entities;

namespace rin.Scene.Components;

public class Component : SceneDisposable
{
    public Entity? Owner;

    protected override void OnDispose(bool isManual)
    {
    }
}