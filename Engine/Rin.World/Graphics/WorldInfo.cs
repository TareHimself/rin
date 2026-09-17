using System.Numerics;
using JetBrains.Annotations;

namespace Rin.World.Graphics;

[NoReorder]
public struct WorldInfo
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPosition;
}