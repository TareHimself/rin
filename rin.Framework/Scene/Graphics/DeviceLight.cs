using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Graphics;

public struct DeviceLight
{
    public Vec3<float> Location;
    public Vec3<float> Direction;
    public Vec4<float> Color;
    public LightType LightType;
}