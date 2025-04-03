using System.Numerics;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.World;
using Rin.Engine.World.Actors;
using Rin.Engine.World.Components;
using Rin.Engine.World.Components.Lights;
using SharpGLTF.Schema2;

namespace rin.Examples.SceneTest;

public static class Extensions
{
    public static async Task<int?> LoadStaticMesh(string filename)
    {
        var model = ModelRoot.Load(filename);

        var mesh = model?.LogicalMeshes.FirstOrDefault();

        if (mesh == null) return null;

        List<MeshSurface> surfaces = [];
        List<uint> indices = [];
        List<Vertex> vertices = [];

        foreach (var primitive in mesh.Primitives)
        {
            if (primitive == null) continue;
            List<Vertex> surfaceVertices = [];
            var newSurface = new MeshSurface
            {
                VertexStart = (uint)vertices.Count,
                VertexCount = (uint)primitive.VertexAccessors.First().Value.Count,
                IndicesStart = (uint)indices.Count,
                IndicesCount = (uint)primitive.IndexAccessor.Count
            };

            var initialVertex = vertices.Count;

            {
                foreach (var idx in primitive.GetIndices()) indices.Add((uint)(idx + initialVertex));
            }

            {
                foreach (var (position, normal, uv) in primitive.GetVertices("POSITION")
                             .AsVector3Array()
                             .Zip(
                                 primitive.GetVertices("NORMAL").AsVector3Array(),
                                 primitive.GetVertices("TEXCOORD_0").AsVector2Array()
                             ))
                    surfaceVertices.Add(new Vertex
                    {
                        Location = position,
                        Normal = normal,
                        UV = uv
                    });
            }

            newSurface.Bounds = surfaceVertices.ComputeBounds();
            vertices.AddRange(surfaceVertices);
            surfaces.Add(newSurface);
        }

        var (id, task) = SGraphicsModule.Get().GetMeshFactory()
            .CreateMesh(vertices.ToBuffer(), indices.ToBuffer(), surfaces.ToArray());
        await task;
        return id;
    }

    public static async Task<Actor?> LoadMeshAsEntity(this World world, string modelPath)
    {
        var meshId = await LoadStaticMesh(modelPath);
        if (meshId == null) return null;
        var entity = world.AddActor(new Actor
        {
            RootComponent = new StaticMeshComponent
            {
                MeshId = meshId.Value
            }
        });
        return entity;
    }

    public static Actor AddPointLight(this World world, Vector3 location)
    {
        var entity = new Actor
        {
            RootComponent = new PointLightComponent
            {
                Location = location,
                Radiance = 10.0f
            }
        };
        world.AddActor(entity);
        return entity;
    }
}