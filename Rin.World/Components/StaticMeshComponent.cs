using System.Numerics;
using Rin.Core.Extensions;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh;

namespace Rin.World.Components;

public class StaticMeshComponent : WorldComponent
{
    public IMeshMaterial?[] Materials = [];
    public StaticMesh? Mesh { get; set; }

    protected override void CollectSelf(CommandList commandList, Matrix4x4 transform)
    {
        if (Mesh is not null && IMeshFactory.Get().GetMesh(Mesh.MeshId) is { } mesh)
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