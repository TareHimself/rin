using Rin.Shade;
using SampleStdlib;
using SampleWorld;

namespace SampleGame;

[Shader]
public class DerivedMeshShader : BaseMeshShader
{
    public override void Compute()
    {
        Bindings.Output.Store(0u, Sd.SdCircle(Push.Location, Push.Center, Push.Radius) + Sd.Dot2(Push.Location));
    }
}
