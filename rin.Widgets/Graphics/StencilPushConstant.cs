using System.Runtime.InteropServices;
using rin.Core.Math;

namespace rin.Widgets.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StencilPushConstant
{
    public Matrix4 Projection;
    public ulong data;
}