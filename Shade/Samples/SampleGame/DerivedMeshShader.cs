using Rin.Shade;
using SampleStdlib;
using SampleWorld;

namespace SampleGame;

[Shader("Fixtures/derived_mesh.slang")]
public class DerivedMeshShader : BaseMeshShader
{
    public override void Compute()
    {
        Push.Output[0] = Sd.SdCircle(Push.Location, Push.Center, Push.Radius) + Sd.Dot2(Push.Location);
    }
}
