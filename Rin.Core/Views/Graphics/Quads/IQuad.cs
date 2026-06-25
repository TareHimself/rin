using System.Numerics;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Graphics.Quads;

public interface IQuad
{
    public Int4 Opts { get; set; }
    public Vector2 Size { get; set; }
    public Matrix4x4 Transform { get; set; }
}