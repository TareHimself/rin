using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.World.Components;

namespace Rin.World.Graphics;

public interface IRenderSystem : IDisposable
{
    RenderProxyHandle CreateStaticMeshProxy(in StaticMeshProxyDesc desc);
    RenderProxyHandle CreateSkinnedMeshProxy(in SkinnedMeshProxyDesc desc);
    RenderProxyHandle CreateLightProxy(in LightInfo desc);
    void UpdateProxyTransform(RenderProxyHandle handle, in Matrix4x4 worldTransform);
    void SetInterpolationAlpha(float alpha);
    void UpdateStaticMeshProxy(RenderProxyHandle handle, in StaticMeshProxyDesc desc);
    void UpdateLightProxy(RenderProxyHandle handle, in LightInfo desc);
    void DestroyProxy(RenderProxyHandle handle);

    IWorldRenderContext Snapshot(CameraComponent view, in Extent2D extent);
    void Build(IGraphBuilder builder, IWorldRenderContext context);
}
