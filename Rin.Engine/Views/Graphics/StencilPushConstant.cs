using System.Numerics;
using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Graphics;

public struct StencilPushConstant
{
    public Matrix4x4 Projection;
    public ulong data;
}