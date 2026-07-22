using System.Numerics;
using Rin.World;
using Rin.World.Actors;
using Rin.World.Components;
using Rin.World.Components.Lights;
using Rin.World.Mesh;
using Rin.World.Mesh.Skinning;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics.Mesh;
using SharpGLTF.Schema2;

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
                foreach (var idx in primitive.GetIndices()) indices.Add(idx - (uint)initialVertex);
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

            newSurface.Bounds = SkinnedVertexExtensions.ComputeBounds(surfaceVertices);
            vertices.AddRange(surfaceVertices);
            surfaces.Add(newSurface);
        }

        var (id, task) = IMeshFactory.Get()
            .CreateMesh(vertices.ToBuffer(), indices.ToBuffer(), surfaces.ToArray());
        await task;
        return new StaticMesh
        {
            MeshId = id
        };
    }

    public static async Task<SkinnedMesh?> LoadSkinnedMesh(string filename)
    {
        var model = ModelRoot.Load(filename);

        var mesh = model?.LogicalMeshes.FirstOrDefault();
        var skin = model?.LogicalSkins.FirstOrDefault();
        if (mesh == null) return null;
        if (skin == null) return null;

        var bones = Enumerable.Range(0, skin.JointsCount).Select(idx =>
        {
            var (joint, _) = skin.GetJoint(idx);
            return new Bone
            {
                Name = joint.Name,
                LocalTransform = Transform.From(joint.LocalMatrix)
            };
        }).ToArray();

        var namesToBones = bones.ToDictionary(c => c.Name, c => c);
        for (var i = 0; i < skin.JointsCount; ++i)
        {
            var (joint, _) = skin.GetJoint(i);
            var bone = bones[i];
            var children = new List<Bone>();
            foreach (var boneChild in joint.VisualChildren)
            {
                var childBone = namesToBones[boneChild.Name];
                childBone.Parent = bone;
                children.Add(childBone);
            }

            bone.Children = children.ToArray();
        }

        var skeleton = new Skeleton(bones, new Pose());
        List<MeshSurface> surfaces = [];
        List<uint> indices = [];
        List<SkinnedVertex> vertices = [];

        foreach (var primitive in mesh.Primitives)
        {
            if (primitive == null) continue;
            List<SkinnedVertex> surfaceVertices = [];
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
                foreach (var (position, normal, uv, jointIndices, weights) in primitive.GetVertices("POSITION")
                             .AsVector3Array()
                             .Zip(
                                 primitive.GetVertices("NORMAL").AsVector3Array(),
                                 primitive.GetVertices("TEXCOORD_0").AsVector2Array(),
                                 primitive.GetVertices("JOINTS_0").GetItemsAsRawBytes(),
                                 primitive.GetVertices("WEIGHTS_0").AsVector4Array()
                             ))
                    surfaceVertices.Add(new SkinnedVertex
                    {
                        Vertex = new Vertex
                        {
                            Location = position,
                            Normal = normal,
                            UV = uv
                        },
                        BoneIndices = new Int4(jointIndices[0], jointIndices[1], jointIndices[2],
                            jointIndices[3]),
                        BoneWeights = weights
                    });
            }

            newSurface.Bounds = SkinnedVertexExtensions.ComputeBounds(surfaceVertices);
            vertices.AddRange(surfaceVertices);
            surfaces.Add(newSurface);
        }

        var (id, task) = IMeshFactory.Get()
            .CreateMesh(vertices.ToBuffer(), indices.ToBuffer(), surfaces.ToArray());
        await task;
        return new SkinnedMesh
        {
            Skeleton = skeleton,
            MeshId = id
        };
    }

    public static async Task<Actor?> LoadMeshAsEntity(this World world, string modelPath)
    {
        var mesh = await LoadStaticMesh(modelPath);
        if (mesh == null) return null;
        var entity = world.AddActor(new Actor
        {
            RootComponent = new StaticMeshComponent
            {
                Mesh = mesh
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