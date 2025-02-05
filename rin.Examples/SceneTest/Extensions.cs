using System.Numerics;
using rin.Editor.Scene.Actors;
using rin.Editor.Scene.Components;
using rin.Editor.Scene.Components.Lights;
using rin.Editor.Scene.Graphics;
using SharpGLTF.Schema2;
using Scene = rin.Editor.Scene.Scene;

namespace rin.Examples.SceneTest;

public static class Extensions
{
    public static async Task<StaticMesh?> LoadStaticMesh(string filename)
    {
        var model = ModelRoot.Load(filename);

        var mesh = model?.LogicalMeshes.FirstOrDefault();

        if (mesh == null) return null;

        List<MeshSurface> surfaces = [];
        List<uint> indices = [];
        List<StaticMesh.Vertex> vertices = [];

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
                        Location = new Vector4(vec.X, vec.Y, vec.Z, 0.0f),
                        Normal = new Vector4(0.0f)
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
        
        return await StaticMesh.Create(surfaces,vertices, indices);
    }
    public static async Task<Actor?> LoadMeshAsEntity(this Scene scene,string modelPath)
    {
        var mesh = await LoadStaticMesh(modelPath);
        if(mesh == null) return null;
        var entity = scene.AddActor(new Actor()
        {
            RootComponent = new StaticMeshComponent()
            {
                Mesh = mesh,
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