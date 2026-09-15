using System.Numerics;

namespace Rin.World.Graphics;

public struct DepthSceneInfo
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
}