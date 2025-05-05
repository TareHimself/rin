using System.Numerics;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Mesh;

namespace Rin.Engine.World.Components;

public class StaticMeshComponent : SceneComponent
{
    public IMeshMaterial?[] Materials = [];
    public StaticMesh? Mesh { get; set; }

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

            commandList.AddStatic(new StaticMeshInfo
            {
                Mesh = mesh,
                Transform = transform,
                SurfaceIndices = Enumerable.Range(0, surfaces.Length).ToArray(),
                Materials = materials.ToArray()
            });
        }
    }
}