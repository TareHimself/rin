using System.Runtime.InteropServices;
using aerox.Runtime.Math;

namespace aerox.Runtime.Widgets.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct StencilPushConstant
{
    public Matrix4 Projection;
    public ulong data;
}