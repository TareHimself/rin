using System.Numerics;

namespace Rin.Framework.Views.Graphics.Vector;

public static class VectorExtensions
{
    public static IPath AddPath(this CommandList self, in Matrix4x4 transform, in Color? color = null)
    {
        return new VectorPath(self, transform, color);
    }
}