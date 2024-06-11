using aerox.Runtime.Math;
using aerox.Runtime.Scene.Components;
using aerox.Runtime.Scene.Entities;
using aerox.Runtime.Scene.Graphics;
using SharpGLTF.Schema2;

namespace SceneTest.entities;

public class MeshEntity : Entity
{
    protected override void CreateDefaultComponents(SceneComponent root)
    {
        base.CreateDefaultComponents(root);
        
        var m1 = AddComponent(new StaticMeshComponent());
        m1.AttachTo(root);
        m1.RelativeTransform = new Transform()
        {
            Location = new Vector3<float>(30.0f, 0.0f, 0.0f)
        };
        
        var m2 = AddComponent(new StaticMeshComponent());
        m2.AttachTo(root);
        m2.RelativeTransform = new Transform()
        {
            Location = new Vector3<float>(-30.0f, 0.0f, 0.0f)
        };
        
        var m3 = AddComponent(new StaticMeshComponent());
        m3.AttachTo(root);
        m3.RelativeTransform = new Transform()
        {
            Location = new Vector3<float>(0.0f, 0.0f, 30.0f)
        };
        
        var m4 = AddComponent(new StaticMeshComponent());
        m4.AttachTo(root);
        m4.RelativeTransform = new Transform()
        {
            Location = new Vector3<float>(0.0f, 0.0f, -30.0f)
        };
        
        
        var meshes = FindComponents<StaticMeshComponent>();
        // using var newMesh = new StaticMesh(
        //     [
        //         new StaticMesh.Vertex()
        //         {
        //             Location = new Vector4<float>(0.0f,0.0f,0.0f,0.0f),
        //             Normal = new Vector4<float>(0.0f,0.0f,0.0f,0.0f),
        //         }
        //     ],
        //     [
        //     ],
        //     [
        //     new MeshSurface()
        //     {
        //         StartIndex = 0,
        //         Count = 6
        //     }]);
        //
        // foreach (var staticMeshComponent in meshes)
        // {
        //     staticMeshComponent.Mesh = newMesh;
        // }
    }

    protected override void OnStart()
    {
        base.OnStart();

        var _ = Task.Run(() => ImportMesh(@"D:\cube.glb"));
    }
    
    
    public void ImportMesh(string filePath)
    {
        var model = ModelRoot.Load(filePath);

        if (model == null) return;

        var mesh = model.LogicalMeshes.FirstOrDefault();

        if (mesh == null) return;

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
        
        var meshes = FindComponents<StaticMeshComponent>();
        using var newMesh = new StaticMesh(vertices.ToArray(),indices.ToArray(),surfaces.ToArray());
        
        foreach (var staticMeshComponent in meshes)
        {
            staticMeshComponent.Mesh = newMesh;
        }
    }
}