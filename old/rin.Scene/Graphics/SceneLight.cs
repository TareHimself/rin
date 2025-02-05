using System.Runtime.InteropServices;
using rin.Runtime.Core.Math;

namespace rin.Scene.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SceneLight
{
    public Vector4<float> Location;
    public Vector4<float> Direction;
    public Vector4<float> Color;
}