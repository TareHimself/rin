using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightInfo
{
    public Vec3<float> Location;
    public Vec3<float> Direction;
    public Vec3<float> Color;
    public float Intensity;
    public LightType LightType;
}