﻿using rin.Editor.Modules;
using rin.Graphics.Material;
using rin.Runtime.Core.Math;
using rin.Scene.Graphics;
using SharpGLTF.Schema2;

namespace rin.Editor.Assets;

public class StaticMeshFactory : AssetFactory
{
    public override Type GetAssetType()
    {
        return typeof(StaticMeshAsset);
    }

    public override Type GetLoadType()
    {
        return typeof(StaticMesh);
    }

    public override object? Load(Asset asset)
    {
        var smAsset = (StaticMeshAsset)asset;
        var matFactory = AssetsModule.Get().FactoryFor<MaterialAsset>();

        return new StaticMesh(smAsset.Vertices, smAsset.Indices, smAsset.Surfaces)
        {
            Materials = smAsset.Materials.Select(m => m == null ? null : (MaterialInstance?)matFactory.Load(m))
                .ToArray()
        };
    }

    public override Task<Asset?> Import(string filePath)
    {
        var model = ModelRoot.Load(filePath);

        if (model == null) return Task.FromResult<Asset?>(null);

        var mesh = model.LogicalMeshes.FirstOrDefault();

        if (mesh == null) return Task.FromResult<Asset?>(null);

        List<MeshSurface> surfaces = new();
        List<uint> indices = new();
        List<StaticMesh.Vertex> vertices = new();

        foreach (var primitive in mesh.Primitives)
        {
            if (primitive == null) continue;
            var newSurface = new MeshSurface
            {
                StartIndex = (uint)indices.Count,
                Count = (uint)primitive.IndexAccessor.Count
            };

            var initialVertex = vertices.Count;

            {
                foreach (var idx in primitive.GetIndices()) indices.Add((uint)(idx + initialVertex));
            }

            {
                foreach (var vec in primitive.GetVertices("POSITION").AsVector3Array())
                    vertices.Add(new StaticMesh.Vertex
                    {
                        Location = new Vector4<float>(vec.X, vec.Y, vec.Z, 0.0f),
                        Normal = new Vector4<float>(0.0f)
                    });
            }

            {
                var idx = initialVertex;
                foreach (var vec in primitive.GetVertices("NORMAL").AsVector3Array())
                {
                    var vert = vertices[idx];
                    vert.Normal.X = vec.X;
                    vert.Normal.Y = vec.Y;
                    vert.Normal.Z = vec.Z;
                    vertices[idx] = vert;
                    idx++;
                }
            }

            {
                var idx = initialVertex;
                foreach (var vec in primitive.GetVertices("TEXCOORD_0").AsVector2Array())
                {
                    var vert = vertices[idx];
                    vert.Location.W = vec.X;
                    vert.Normal.W = vec.Y;
                    vertices[idx] = vert;
                    idx++;
                }
            }

            surfaces.Add(newSurface);
        }

        return Task.FromResult<Asset?>(new StaticMeshAsset(surfaces.ToArray())
        {
            Vertices = vertices.ToArray(),
            Indices = indices.ToArray()
        });
    }

    public override Task<bool> Export(string filePath, Asset asset)
    {
        throw new NotImplementedException();
    }

    public override bool CanImport()
    {
        return true;
    }

    public override bool CanExport()
    {
        return false;
    }
}