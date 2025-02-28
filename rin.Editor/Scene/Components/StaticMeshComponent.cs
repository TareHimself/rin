﻿using Rin.Editor.Scene.Graphics;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Core.Math;

namespace Rin.Editor.Scene.Components;

public class StaticMeshComponent : SceneComponent
{
    public IStaticMesh? Mesh { get; set; }
    public IMeshMaterial?[] Materials = [];
    protected override void CollectSelf(DrawCommands drawCommands, Mat4 transform)
    {
        if (Mesh is not { } mesh) return;
        for (var i = 0; i < mesh.Surfaces.Length; i++)
        {
            var material = Materials.TryGet(i) ?? mesh.Materials.TryGet(i) ?? DefaultMeshMaterial.DefaultMesh;
            var surface = mesh.Surfaces[i];
            drawCommands.AddCommand(new GeometryInfo
            {
                MeshMaterial = material,
                Geometry = mesh.Geometry,
                Surface = surface,
                Transform = transform
            });
        }
    }
}