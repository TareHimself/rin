using aerox.Runtime.Archives;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using aerox.Runtime.Scene;
using aerox.Runtime.Scene.Components;
using aerox.Runtime.Scene.Components.Lights;
using aerox.Runtime.Scene.Entities;
using aerox.Runtime.Scene.Graphics;
using aerox.Runtime.Widgets;
using SharpGLTF.Schema2;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Image = SixLabors.ImageSharp.Image;
using Texture = aerox.Runtime.Graphics.Texture;

namespace SceneTest.entities;

public class MeshEntity : Entity
{
    protected override void CreateDefaultComponents(SceneComponent root)
    {
        base.CreateDefaultComponents(root);
        var dist = 5.0f;
        var m1 = AddComponent<StaticMeshComponent>();
        m1.AttachTo(root);
        m1.SetRelativeLocation(new Vector3<float>(dist, 0.0f, 0.0f));
        
        var m2 = AddComponent<StaticMeshComponent>();
        m2.AttachTo(root);
        m2.SetRelativeLocation(new Vector3<float>(-dist, 0.0f, 0.0f));
        
        var m3 = AddComponent<StaticMeshComponent>();
        m3.AttachTo(root);
        m3.SetRelativeLocation(new Vector3<float>(0.0f, 0.0f, dist));
        
        var m4 = AddComponent<StaticMeshComponent>();
        m4.AttachTo(root);
        m4.SetRelativeLocation(new Vector3<float>(0.0f, 0.0f, -dist));

        var light = AddComponent<PointLightComponent>();
        light.Intensity = 30.0f;
        light.Color = Color.White;
        light.Radius = 20000.0f;
        light.SetRelativeLocation(new Vector3<float>(0.0f,20.0f,0.0f));
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

    protected override void OnTick(double deltaSeconds)
    {
        base.OnTick(deltaSeconds);
        var root = RootComponent!;
        var newRotation = root.GetRelativeRotation().ApplyYaw((float)deltaSeconds * 100.0f);
        root.SetRelativeRotation(newRotation);
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


        using var ar = new ZipArchiveReader(@"D:\gold\gold.pbr");
        var meshMaterial = new MaterialBuilder().ConfigureForScene().SetShader(SGraphicsModule.Get()
            .LoadShader(Path.Join(SSceneModule.ShadersDir, "mesh_test.ash"))).Build();
        
        foreach (var arKey in ar.Keys)
        {
            var image = Image.Load<Rgba32>(ar.CreateReadStream(arKey));
            
            using var tex = new Texture(image.ToBytes(), new VkExtent3D()
            {
                width = (uint)image.Width,
                height = (uint)image.Height,
                depth = 1
            }, EImageFormat.Rgba8UNorm, EImageFilter.Linear, EImageTiling.ClampEdge);
            var id = arKey.Split(".")[0];
            meshMaterial.BindTexture(id, tex);
        }
        
        Console.WriteLine("Imported Textures");
        var meshes = FindComponents<StaticMeshComponent>();
        using var newMesh = new StaticMesh(vertices.ToArray(),indices.ToArray(),surfaces.ToArray())
        {
            Materials = [meshMaterial]
        };
        
        foreach (var staticMeshComponent in meshes)
        {
            staticMeshComponent.Mesh = newMesh;
        }
    }
}