using System.Numerics;

namespace Rin.Engine.Views.Graphics;

public struct StencilPushConstant
{
    public Matrix4x4 Projection;
    public ulong data;
}