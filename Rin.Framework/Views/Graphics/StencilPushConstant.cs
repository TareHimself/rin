using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Framework.Views.Graphics;

[NoReorder]
public struct StencilPushConstant
{
    public Matrix4x4 Projection;
    public ulong data;
}