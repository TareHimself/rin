using System.Numerics;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Graphics.Default;
using Rin.Engine.World.Mesh.Skinning;

namespace Rin.Engine.World.Components;

public class SkinnedMeshComponent : SceneComponent
{
    public IMeshMaterial?[] Materials = [];
    public SkinnedMesh? Mesh { get; set; }
    public IPoseSource? PoseSource { get; set; }

    protected override void CollectSelf(CommandList commandList, Matrix4x4 transform)
    {
        if (Mesh is not null && SGraphicsModule.Get().GetMeshFactory().GetMesh(Mesh.MeshId) is { } mesh)
        {
            var surfaces = mesh.GetSurfaces();
            IMeshMaterial lastMaterial = DefaultMeshMaterial.DefaultMesh;
            List<IMeshMaterial> materials = [];
            for (var i = 0; i < surfaces.Length; i++)
            {
                var material = lastMaterial = Materials.TryGet(i) ?? lastMaterial;
                materials.Add(material);
            }

            commandList.AddSkinned(new SkinnedMeshInfo
            {
                Skeleton = Mesh.Skeleton,
                Pose = PoseSource?.GetPose() ?? Mesh.Skeleton.BasePose,
                Mesh = mesh,
                Transform = transform,
                SurfaceIndices = Enumerable.Range(0, surfaces.Length).ToArray(),
                Materials = materials.ToArray()
            });
        }
    }
}