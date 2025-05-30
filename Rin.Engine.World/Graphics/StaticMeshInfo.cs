﻿using System.Numerics;
using Rin.Engine.Graphics.Meshes;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Contains info about a static mesh to be drawn in the scene
/// </summary>
public class StaticMeshInfo : IMeshCommand
{
    public required Matrix4x4 Transform;

    public bool CastShadow { get; set; }
    public required int[] SurfaceIndices { get; init; }
    public required IMeshMaterial[] Materials { get; init; }
    public required IMesh Mesh { get; init; }
}