using System.Numerics;
using System.Runtime.CompilerServices;
using Rin.Shade;

namespace Rin.Shade.Tests.Fixtures;

public enum BlendMode
{
    Opaque = 0,
    Translucent = 1
}

[ShaderStruct]
public struct MeshData
{
    public Vector4 ColorAndTextureId;
}

[InlineArray(6)]
public struct SixVector4s
{
    private Vector4 _element;
}

public struct Frustum
{
    public SixVector4s Planes;
}

public struct CorrectnessPushConstants
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public MeshData Mesh;
    public Frustum Frustum;
    public BlendMode Mode;
    public BufferRef<float> Output;
}

public static class CorrectnessHelpers
{
    public static void Split(float value, out float whole, out float frac)
    {
        whole = value;
        frac = value - whole;
    }
}

[Shader("Fixtures/correctness.slang")]
public class CorrectnessFixtureShader : Shader
{
    [Push] protected CorrectnessPushConstants Push;

    [Compute(1, 1, 1)]
    public void Compute()
    {
        var viewProjection = Push.View * Push.Projection;
        var isOpaque = Push.Mode == BlendMode.Opaque;

        float whole;
        float frac;
        CorrectnessHelpers.Split(1.5f, out whole, out frac);

        Push.Output[0] = Push.Mesh.ColorAndTextureId.X + whole + frac;
    }
}
