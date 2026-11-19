using System.Numerics;
using Rin.Core.Graphics;
using Rin.World.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Mesh.Skinning;

namespace Rin.GLTF.Tests;

/// <summary>
///     Hands out proxy handles and ignores everything else, for tests that only read the world.
/// </summary>
internal sealed class NullRenderSystem : IRenderSystem
{
    private uint _nextIndex;

    public RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc) => Create();
    public RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc) => Create();
    public RenderProxyHandle CreateLightProxy(in LightInfo desc) => Create();
    public RenderProxyHandle CreateEnvironmentProxy(in EnvironmentVolume desc) => Create();

    public void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform)
    {
    }

    public void UpdateSkinnedProxyPose(RenderProxyHandle handle, in SkeletalPose pose)
    {
    }

    public void SetInterpolationAlpha(float alpha)
    {
    }

    public void SetTime(float seconds)
    {
    }

    public void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc)
    {
    }

    public void UpdateEnvironmentProxy(RenderProxyHandle handle, in EnvironmentVolume desc)
    {
    }

    public void DestroyProxy(RenderProxyHandle handle)
    {
    }

    public IWorldCollectedData Snapshot(in CameraView view, in Extent2D extent) => throw new NotSupportedException();

    public void Dispose()
    {
    }

    private RenderProxyHandle Create() => new(++_nextIndex, 1);
}
