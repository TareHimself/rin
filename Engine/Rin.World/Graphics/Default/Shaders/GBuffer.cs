using System.Numerics;
using Rin.Core.Graphics;

namespace Rin.World.Graphics.Default.Shaders;

public struct GBufferHandles
{
    public DeviceHandle GBuffer0;
    public DeviceHandle GBuffer1;
    public DeviceHandle GBuffer2;
    public DeviceHandle GBuffer3;
}

public struct GBufferSample
{
    public Vector3 Color;
    public Vector3 Location;
    public Vector3 Normal;
    public float Roughness;
    public float Metallic;
    public float Specular;
    public float Emissive;
}
