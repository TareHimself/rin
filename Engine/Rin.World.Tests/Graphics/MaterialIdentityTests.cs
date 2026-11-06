using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;

namespace Rin.World.Tests.Graphics;

public class MaterialIdentityTests
{
    [Test]
    public void SamePassTypeWithTheSameShaderHasTheSameIdentity()
    {
        var shader = new FakeShader();

        Assert.That(MaterialIdentity.Of(new PassA(shader)), Is.EqualTo(MaterialIdentity.Of(new PassA(shader))));
    }

    [Test]
    public void DifferentPassTypesWithTheSameShaderDoNotBatch()
    {
        var shader = new FakeShader();

        Assert.That(MaterialIdentity.Of(new PassA(shader)), Is.Not.EqualTo(MaterialIdentity.Of(new PassB(shader))));
    }

    [Test]
    public void SamePassTypeWithDifferentShadersDoesNotBatch()
    {
        Assert.That(MaterialIdentity.Of(new PassA(new FakeShader())),
            Is.Not.EqualTo(MaterialIdentity.Of(new PassA(new FakeShader()))));
    }

    private class PassA(IGraphicsShader shader) : IMaterialPass
    {
        public IGraphicsShader Shader => shader;
        public ulong GetRequiredMemory() => 0;
        public void Write(Span<byte> destination, ProcessedMesh mesh) => throw new NotSupportedException();

        public IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer) =>
            throw new NotSupportedException();

        public void DeclareResources(IGraphConfig config, ProcessedMesh mesh) => throw new NotSupportedException();
    }

    private sealed class PassB(IGraphicsShader shader) : PassA(shader);

    private sealed class FakeShader : IGraphicsShader
    {
        public bool Ready => true;
        public ImageFormat[] AttachmentFormats => [];
        public BlendState BlendState => BlendState.Opaque;
        public bool UsesStencil => false;
        public bool UsesDepth => true;
        public void Compile(ICompilationContext context) => throw new NotSupportedException();
        public IGraphicsBindContext? Bind(IExecutionContext ctx, bool wait = true) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}
