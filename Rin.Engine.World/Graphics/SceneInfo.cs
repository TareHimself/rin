using System.Numerics;

namespace Rin.Engine.World.Graphics;

public struct SceneInfo
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPosition;
    public ulong LightsAddress;
    public int NumLights;
}