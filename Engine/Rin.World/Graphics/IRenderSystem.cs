using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Graphics;

public interface IRenderSystem : IDisposable
{
    RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc);
    RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc);
    RenderProxyHandle CreateLightProxy(in LightInfo desc);
    RenderProxyHandle CreateEnvironmentProxy(in EnvironmentVolume desc);

    /// <summary>
    ///     Called before a frame's transforms are pushed. After a tick they blend from the last tick's pose by the
    ///     interpolation alpha. With no tick, a pushed transform was changed outside the simulation, so it is drawn at once.
    /// </summary>
    void BeginFrame(bool ticked);

    void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform);
    void UpdateSkinnedProxyPose(RenderProxyHandle handle, in SkeletalPose pose);
    void SetInterpolationAlpha(float alpha);

    /// <summary>
    ///     Seconds of scaled world time, which surface materials read to animate vertices.
    /// </summary>
    void SetTime(float seconds);
    void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc);
    void UpdateEnvironmentProxy(RenderProxyHandle handle, in EnvironmentVolume desc);
    void DestroyProxy(RenderProxyHandle handle);

    IWorldCollectedData Snapshot(in CameraView view, in Extent2D extent);
}
