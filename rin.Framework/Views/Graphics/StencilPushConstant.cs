using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics;


public struct StencilPushConstant
{
    public Mat4 Projection;
    public ulong data;
}