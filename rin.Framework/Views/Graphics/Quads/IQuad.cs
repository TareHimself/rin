using System.Numerics;
using rin.Framework.Core.Math;

namespace rin.Framework.Views.Graphics.Quads;

public interface IQuad
{
    public Vec4<int> Opts { get; set; }
    public Vector2 Size { get; set; }
    public Mat3 Transform { get; set; }
}