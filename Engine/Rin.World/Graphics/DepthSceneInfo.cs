using System.Numerics;
using JetBrains.Annotations;

namespace Rin.World.Graphics;
[NoReorder]
public struct DepthSceneInfo
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public float Time;
}