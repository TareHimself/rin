namespace Rin.World.Mesh.Skinning;

public record AnimationNotify(float Time, IAnimationNotifyFactory Factory, bool DominantClipOnly = false);
