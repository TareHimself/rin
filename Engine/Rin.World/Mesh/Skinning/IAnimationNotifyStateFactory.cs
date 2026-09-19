namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotifyStateFactory
{
    IAnimationNotifyState Create();
    void Release(IAnimationNotifyState instance);
}
