using Rin.Engine.Core.Math;

namespace Rin.Engine.Views.Graphics;

public struct StencilPushConstant
{
    public Mat4 Projection;
    public ulong data;
}