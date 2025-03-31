using System.Numerics;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.World.Graphics;

namespace Rin.Engine.World.Components;

public class StaticMeshComponent : SceneComponent
{
    public int? MeshId { get; set; }
    public IMeshMaterial?[] Materials = [];
    protected override void CollectSelf(DrawCommands drawCommands, Matrix4x4 transform)
    {
        if (MeshId != null && SGraphicsModule.Get().GetMeshFactory().GetMesh(MeshId.Value) is { } mesh)
        {
            var surfaces = mesh.GetSurfaces();
            for (var i = 0; i < surfaces.Length; i++)
            {
                var material = Materials.TryGet(i) ?? DefaultMeshMaterial.DefaultMesh;
                drawCommands.AddCommand(new GeometryInfo
                {
                    MeshMaterial = material,
                    Mesh = mesh,
                    SurfaceIndex = i,
                    Transform = transform
                });
            }
        }
    }
}