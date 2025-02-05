using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Editor.Scene.Graphics;


public struct DepthSceneInfo
{
    public Mat4 View;
    public Mat4 Projection;
    public Mat4 ViewProjection;
}