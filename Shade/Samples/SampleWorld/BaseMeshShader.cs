using System.Numerics;
using Rin.Shade;
using SampleStdlib;

namespace SampleWorld;

public struct MeshPushConstants
{
    public Vector2 Location;
    public Vector2 Center;
    public float Radius;
    public BufferRef<float> Output;
}

[ShadeExport]
public abstract class BaseMeshShader : Shader
{
    [Push] protected MeshPushConstants Push;

    [Compute(1, 1, 1)]
    public virtual void Compute()
    {
        Push.Output[0] = Sd.SdCircle(Push.Location, Push.Center, Push.Radius);
    }
}
