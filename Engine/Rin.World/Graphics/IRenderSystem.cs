using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Components;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics;

public interface IRenderSystem : IDisposable
{
    RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc);
    RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc);
    RenderProxyHandle CreateLightProxy(in LightInfo desc);
    void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform);
    void UpdateSkinnedProxyPose(RenderProxyHandle handle, in SkeletalPose pose);
    void SetInterpolationAlpha(float alpha);
    void UpdateStaticMeshProxy(RenderProxyHandle handle, in StaticMeshProxyDesc desc);
    void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc);
    void DestroyProxy(RenderProxyHandle handle);

    IWorldCollectedData Snapshot(CameraComponent view, in Extent2D extent);
}
