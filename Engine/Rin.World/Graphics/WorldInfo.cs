using System.Numerics;

namespace Rin.World.Graphics;

public struct WorldInfo
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPosition;
}