namespace Rin.World.Mesh.Skinning;

public interface IAnimationNotifyFactory
{
    IAnimationNotify Create();
    void Release(IAnimationNotify instance);
}
