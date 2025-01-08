using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DeviceLight
{
    public Vec3<float> Location;
    public Vec3<float> Direction;
    public Vec4<float> Color;
    public LightType LightType;
}