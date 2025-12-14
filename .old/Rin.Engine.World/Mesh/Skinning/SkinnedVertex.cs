using System.Numerics;
using Rin.Framework.Graphics.Meshes;
using Rin.Framework.Shared.Math;

namespace Rin.Engine.World.Mesh.Skinning;

/// <summary>
///     4 bones per vertex
/// </summary>
public struct SkinnedVertex : IVertex
{
    public Vertex Vertex;
    public Int4 BoneIndices;
    public Vector4 BoneWeights;
}