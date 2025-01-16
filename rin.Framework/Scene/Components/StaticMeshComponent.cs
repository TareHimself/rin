using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Scene.Graphics;

namespace rin.Framework.Scene.Components;

public class StaticMeshComponent : RenderedComponent
{
    public IStaticMesh? Mesh { get; set; }
    public IMaterial?[] Materials = [];
    protected override void CollectSelf(DrawCommands drawCommands, Mat4 transform)
    {
        if (Mesh is not { } mesh) return;
        for (var i = 0; i < mesh.Surfaces.Length; i++)
        {
            var material = Materials.TryGet(i) ?? mesh.Materials.TryGet(i) ?? DefaultMaterial.Default;
            var surface = mesh.Surfaces[i];
            drawCommands.AddCommand(new GeometryInfo
            {
                Material = material,
                Geometry = mesh.Geometry,
                Surface = surface,
                Transform = transform
            });
        }
    }
}