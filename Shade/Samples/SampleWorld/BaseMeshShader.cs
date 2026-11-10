using System.Numerics;
using Rin.Shade;
using SampleStdlib;

namespace SampleWorld;

public struct MeshPushConstants
{
    public Vector2 Location;
    public Vector2 Center;
    public float Radius;
}

public struct MeshBindings
{
    public RWByteAddressBuffer Output;
}

[ShadeExport]
public abstract class BaseMeshShader : Shader
{
    [Push] protected MeshPushConstants Push;
    [BindingGroup] protected static MeshBindings Bindings;

    [Compute(1, 1, 1)]
    public virtual void Compute()
    {
        Bindings.Output.Store(0u, Sd.SdCircle(Push.Location, Push.Center, Push.Radius));
    }
}
