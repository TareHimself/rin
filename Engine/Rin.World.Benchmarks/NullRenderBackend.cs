using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using Rin.World.Graphics;
using Rin.World.Graphics.ImageBasedLighting;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Benchmarks;

/// <summary>
///     A render system that only hands out proxy handles, so a benchmark times the world's side of extract.
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

    public void BeginFrame(bool ticked)
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

/// <summary>
///     A mesh factory with one mesh that has no GPU data.
/// </summary>
internal sealed class NullMeshFactory : IMeshFactory
{
    public static readonly MeshHandle Mesh = new(0, 1);

    private readonly NullMesh _mesh = new();

    public MeshHandle CreateMesh<TVertexFormat>(ReadOnlySpan<TVertexFormat> vertices, ReadOnlySpan<uint> indices,
        MeshSurface[] surfaces) where TVertexFormat : unmanaged, IVertex => throw new NotSupportedException();

    public IMesh? GetMesh(MeshHandle mesh) => mesh == Mesh ? _mesh : null;

    public void FreeMeshes(params ReadOnlySpan<MeshHandle> meshes)
    {
    }

    public void Dispose()
    {
    }

    private sealed class NullMesh : IMesh
    {
        public MeshSurface[] GetSurfaces() => [];

        public MeshSurface GetSurface(int surfaceIndex) => throw new NotSupportedException();

        public DeviceBufferView GetVertices() => default;

        public DeviceBufferView GetVertices(int surfaceIndex) => default;

        public uint GetVertexCount(int surfaceIndex) => 0;

        public DeviceBufferView GetIndices() => default;

        public Bounds3D GetBounds() => default;
    }
}
