using System.Numerics;
using Rin.Engine.Scene.Actors;
using Rin.Engine.Scene.Components;
using Rin.Engine.Scene.Components.Lights;
using Rin.Engine.Core.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Meshes;
using SharpGLTF.Schema2;
using Scene = Rin.Engine.Scene.Scene;

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
            
            var newSurface = new MeshSurface
            {
                VertexIndex = (uint)vertices.Count,
                VertexCount = (uint)primitive.VertexAccessors.First().Value.Count,
                IndicesCount = (uint)primitive.IndexAccessor.Count
            };

            var initialVertex = vertices.Count;

            {
                foreach (var idx in primitive.GetIndices()) indices.Add((uint)(idx + initialVertex));
            }

            {
                foreach (var (position,normal,uv) in primitive.GetVertices("POSITION")
                             .AsVector3Array()
                             .Zip(
                                 primitive.GetVertices("NORMAL").AsVector3Array(),
                                 primitive.GetVertices("TEXCOORD_0").AsVector2Array()
                                 ))
                    vertices.Add(new Vertex
                    {
                        Location = position,
                        Normal = normal,
                        UV = uv
                    });
            }

            surfaces.Add(newSurface);
        }

        var (id, task) = SGraphicsModule.Get().GetMeshFactory()
            .CreateMesh(vertices.ToBuffer(), indices.ToBuffer(), surfaces.ToArray());
        await task;
        return id;
    }
    public static async Task<Actor?> LoadMeshAsEntity(this Scene scene,string modelPath)
    {
        var meshId = await LoadStaticMesh(modelPath);
        if(meshId == null) return null;
        var entity = scene.AddActor(new Actor()
        {
            RootComponent = new StaticMeshComponent()
            {
                MeshId = meshId.Value,
            }
        });
        return entity;
    }
    
    public static Actor AddPointLight(this Scene scene,Vector3 location)
    {
        var entity = new Actor()
        {
            RootComponent = new PointLightComponent()
            {
                Location = location,
                Radiance = 10.0f
            },
        };
        scene.AddActor(entity);
        return entity;
    }
}