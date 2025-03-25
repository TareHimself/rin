using Rin.Editor.Scene.Graphics;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;

namespace Rin.Editor.Scene.Components;

public class StaticMeshComponent : SceneComponent
{
    public int? MeshId { get; set; }
    public IMeshMaterial?[] Materials = [];
    protected override void CollectSelf(DrawCommands drawCommands, Mat4 transform)
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