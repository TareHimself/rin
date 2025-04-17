using System.Numerics;
using Rin.Engine.Graphics.Meshes;
using Rin.Engine.Math;

namespace Rin.Engine.World.Mesh.Skinning;

/// <summary>
/// 4 bones per vertex
/// </summary>
public struct SkinnedVertex : IVertex
{
    public Vertex Vertex;
    public Vector4<int> BoneIndices;
    public Vector4 BoneWeights;
}