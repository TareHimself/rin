using aerox.Runtime.Scene.Components;
using aerox.Runtime.Scene.Entities;

namespace SceneTest.entities;

public class CameraEntity : Entity
{
    public CameraComponent? CameraComp;
    protected override SceneComponent CreateRootComponent()
    {
        CameraComp = new CameraComponent();
        return CameraComp;
    }
}