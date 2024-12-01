using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Framework.Widgets.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StencilPushConstant
{
    public Matrix4 Projection;
    public ulong data;
}