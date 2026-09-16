using System.Numerics;
using Rin.Core.Extensions;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Math;
using Rin.World.Mesh.Skinning;

namespace Rin.World.Components;

public class SkinnedMeshComponent : WorldComponent
{
    public IMeshMaterial?[] Materials = [];
    public SkinnedMesh? Mesh { get; set; }
    public IPoseSource? PoseSource { get; set; }

    private RenderProxyHandle _proxy = RenderProxyHandle.Invalid;
    private uint _lastPushedVersion;

    public override void Start()
    {
        base.Start();
        if (Mesh is not null && IMeshFactory.Get().GetMesh(Mesh.MeshId) is { } mesh)
        {
            var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh);
            _proxy = Owner!.World!.RenderSystem.CreateSkinnedMeshProxy(new SkinnedMeshProxyDesc
            {
                Skeleton = Mesh.Skeleton,
                Pose = PoseSource?.GetPose() ?? Mesh.Skeleton.BasePose,
                Mesh = mesh,
                Transform = GetTransform(Space.World).ToMatrix(),
                SurfaceIndices = surfaceIndices,
                Materials = materials
            });
            _lastPushedVersion = TransformVersion;
        }
    }

    public override void Stop()
    {
        if (_proxy.IsValid)
        {
            Owner!.World!.RenderSystem.DestroyProxy(_proxy);
            _proxy = RenderProxyHandle.Invalid;
        }

        base.Stop();
    }

    public override void LateUpdate(float deltaSeconds)
    {
        base.LateUpdate(deltaSeconds);
        if (!_proxy.IsValid) return;
        var worldTransform = GetTransform(Space.World);
        if (TransformVersion != _lastPushedVersion)
        {
            Owner!.World!.RenderSystem.UpdateProxyTransform(_proxy, worldTransform.ToMatrix());
            _lastPushedVersion = TransformVersion;
        }
    }

    protected override void CollectSelf(CommandList commandList, Matrix4x4 transform)
    {
        if (Mesh is not null && IMeshFactory.Get().GetMesh(Mesh.MeshId) is { } mesh)
        {
            var (surfaceIndices, materials) = ResolveSurfaceMaterials(mesh);
            commandList.AddSkinned(new SkinnedMeshInfo
            {
                Skeleton = Mesh.Skeleton,
                Pose = PoseSource?.GetPose() ?? Mesh.Skeleton.BasePose,
                Mesh = mesh,
                Transform = transform,
                SurfaceIndices = surfaceIndices,
                Materials = materials
            });
        }
    }

    private (int[] SurfaceIndices, IMeshMaterial[] Materials) ResolveSurfaceMaterials(IMesh mesh)
    {
        var surfaces = mesh.GetSurfaces();
        IMeshMaterial lastMaterial = DefaultMeshMaterial.DefaultMesh;
        List<IMeshMaterial> materials = [];
        for (var i = 0; i < surfaces.Length; i++)
        {
            var material = lastMaterial = Materials.TryGet(i) ?? lastMaterial;
            materials.Add(material);
        }

        return (Enumerable.Range(0, surfaces.Length).ToArray(), materials.ToArray());
    }
}
