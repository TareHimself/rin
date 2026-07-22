using System.Numerics;
using Rin.Core.Shared.Math;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Mesh.Skinning;

/// <summary>
///     4 bones per vertex
/// </summary>
public struct SkinnedVertex : IVertex
{
    public Vertex Vertex;
    public Int4 BoneIndices;
    public Vector4 BoneWeights;
}