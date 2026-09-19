using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics.Mesh;
using Rin.World.Mesh;
using Rin.World.Mesh.Skinning;
using SharpGLTF.Schema2;

namespace Rin.GLTF;

public static class GltfMeshImporter
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
            var positions = primitive.GetVertices("POSITION").AsVector3Array();
            var localIndices = ReadOrSynthesizeIndices(primitive, positions.Count);
            var normals = ReadOrComputeNormals(primitive, positions, localIndices);

            List<Vertex> surfaceVertices = [];
            var newSurface = new MeshSurface
            {
                VertexStart = (uint)vertices.Count,
                VertexCount = (uint)positions.Count,
                IndicesStart = (uint)indices.Count,
                IndicesCount = (uint)localIndices.Length
            };

            var initialVertex = vertices.Count;

            {
                foreach (var idx in localIndices) indices.Add(idx - (uint)initialVertex);
            }

            {
                foreach (var (position, normal, uv) in positions
                             .Zip(
                                 normals,
                                 primitive.GetVertices("TEXCOORD_0").AsVector2Array()
                             ))
                    surfaceVertices.Add(new Vertex
                    {
                        Location = position,
                        Normal = normal,
                        UV = uv
                    });
            }

            newSurface.Bounds = CollectionsMarshal.AsSpan(surfaceVertices).ComputeBounds();
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

        // Bones come only from skin.joints (true joints per the glTF spec) - never from a wrapping
        // scene-graph node - so an "Armature" container never leaks in as a fake root bone.
        var bones = Enumerable.Range(0, skin.JointsCount).Select(idx =>
        {
            var (joint, inverseBindMatrix) = skin.GetJoint(idx);
            return new Bone
            {
                Name = joint.Name,
                LocalTransform = Transform.From(joint.LocalMatrix),
                Bind = inverseBindMatrix
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

        var skeleton = new Skeleton(bones);
        List<MeshSurface> surfaces = [];
        List<uint> indices = [];
        List<SkinnedVertex> vertices = [];

        foreach (var primitive in mesh.Primitives)
        {
            if (primitive == null) continue;
            var positions = primitive.GetVertices("POSITION").AsVector3Array();
            var localIndices = ReadOrSynthesizeIndices(primitive, positions.Count);
            var normals = ReadOrComputeNormals(primitive, positions, localIndices);

            List<SkinnedVertex> surfaceVertices = [];
            var newSurface = new MeshSurface
            {
                VertexStart = (uint)vertices.Count,
                VertexCount = (uint)positions.Count,
                IndicesStart = (uint)indices.Count,
                IndicesCount = (uint)localIndices.Length
            };

            var initialVertex = vertices.Count;

            {
                foreach (var idx in localIndices) indices.Add((uint)(idx + initialVertex));
            }

            {
                foreach (var (position, normal, uv, jointIndices, weights) in positions
                             .Zip(
                                 normals,
                                 primitive.GetVertices("TEXCOORD_0").AsVector2Array(),
                                 // JOINTS_0 is UNSIGNED_BYTE or UNSIGNED_SHORT depending on the exporter (this
                                 // asset uses SHORT) - AsVector4Array() decodes either correctly, unlike reading
                                 // GetItemsAsRawBytes() directly, which silently misreads SHORT-encoded joints.
                                 primitive.GetVertices("JOINTS_0").AsVector4Array(),
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
                        BoneIndices = new Int4((int)jointIndices.X, (int)jointIndices.Y, (int)jointIndices.Z,
                            (int)jointIndices.W),
                        BoneWeights = weights
                    });
            }

            newSurface.Bounds = CollectionsMarshal.AsSpan(surfaceVertices).ComputeBounds();
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

    private static uint[] ReadOrSynthesizeIndices(MeshPrimitive primitive, int vertexCount)
    {
        if (primitive.IndexAccessor is null) return Enumerable.Range(0, vertexCount).Select(i => (uint)i).ToArray();

        return primitive.GetIndices().ToArray();
    }

    // Flat per-triangle normals, averaged per vertex - a reasonable default for content that omits
    // NORMAL entirely (valid per the glTF spec), not a substitute for authored smoothing groups.
    private static Vector3[] ReadOrComputeNormals(MeshPrimitive primitive, IReadOnlyList<Vector3> positions,
        IReadOnlyList<uint> indices)
    {
        if (primitive.GetVertexAccessor("NORMAL") is not null) return primitive.GetVertices("NORMAL").AsVector3Array().ToArray();

        var normals = new Vector3[positions.Count];
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var a = (int)indices[i];
            var b = (int)indices[i + 1];
            var c = (int)indices[i + 2];
            var faceNormal = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            normals[a] += faceNormal;
            normals[b] += faceNormal;
            normals[c] += faceNormal;
        }

        for (var i = 0; i < normals.Length; i++)
            normals[i] = normals[i] == Vector3.Zero ? Vector3.UnitY : Vector3.Normalize(normals[i]);

        return normals;
    }
}
