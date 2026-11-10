using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Runs material structs plugged into a generic base shader through interface members, since the emitter
///     tests only check the text.
/// </summary>
public class InterfaceDispatchTests
{
    private const string Source = """
        using System.Numerics;
        using Rin.Shade;
        using Rin.Shade.Tests;

        namespace DispatchCpuCheck;

        public struct Surface
        {
            public Vector3 Color;
            public float Roughness;
        }

        public interface ISurfaceMaterial<TSelf> where TSelf : unmanaged, ISurfaceMaterial<TSelf>
        {
            static abstract Surface Evaluate(in TSelf material, Vector2 uv);

            float Brightness();

            static virtual Vector3 Offset(in TSelf material, Vector3 world) => new Vector3(0f);
        }

        public struct Gold : ISurfaceMaterial<Gold>
        {
            public Vector3 Tint;
            public float Roughness;

            public static Surface Evaluate(in Gold material, Vector2 uv)
            {
                Surface surface;
                surface.Color = material.Tint * uv.X;
                surface.Roughness = material.Roughness;
                return surface;
            }

            public float Brightness() => Tint.X + Tint.Y + Tint.Z;

            public static Vector3 Offset(in Gold material, Vector3 world) =>
                new Vector3(0f, material.Roughness * world.X, 0f);
        }

        public struct Plain : ISurfaceMaterial<Plain>
        {
            public float Value;

            public static Surface Evaluate(in Plain material, Vector2 uv)
            {
                Surface surface;
                surface.Color = new Vector3(material.Value);
                surface.Roughness = 1f;
                return surface;
            }

            public float Brightness() => Value;
        }

        public struct SurfacePush<TMaterial> where TMaterial : unmanaged
        {
            public TestBuffer<TMaterial> Materials;
            public TestBuffer<float> Output;
        }

        public abstract class SurfaceShader<TMaterial> : Shader
            where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
        {
            [Push] protected SurfacePush<TMaterial> Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                var material = Push.Materials[0];
                var surface = TMaterial.Evaluate(material, new Vector2(0.5f, 0f));
                Push.Output[0] = surface.Color.X + surface.Color.Y + surface.Color.Z + surface.Roughness;
                Push.Output[1] = material.Brightness();
                Push.Output[2] = TMaterial.Offset(material, new Vector3(1f, 2f, 3f)).Y;
            }
        }

        [Shader]
        public class GoldShader : SurfaceShader<Gold>;

        [Shader]
        public class PlainShader : SurfaceShader<Plain>;
        """;

    [StructLayout(LayoutKind.Sequential)]
    private struct Gold
    {
        public Vector3 Tint;
        public float Roughness;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Push<TMaterial> where TMaterial : unmanaged
    {
        public TestBuffer<TMaterial> Materials;
        public TestBuffer<float> Output;
    }

    [Test]
    public void MaterialStructsDispatchThroughTheirInterfaceOnTheCpu()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);

        var gold = new Gold { Tint = new Vector3(1f, 2f, 3f), Roughness = 0.5f };
        var plain = 4f;
        var goldOutput = Run(result.Shaders["GoldShader"], gold);
        var plainOutput = Run(result.Shaders["PlainShader"], plain);

        Assert.Multiple(() =>
        {
            Assert.That(goldOutput[0], Is.EqualTo(0.5f + 1f + 1.5f + 0.5f).Within(1e-5f), "Gold static abstract");
            Assert.That(goldOutput[1], Is.EqualTo(6f).Within(1e-5f), "Gold instance member");
            Assert.That(goldOutput[2], Is.EqualTo(0.5f).Within(1e-5f), "Gold static virtual override");
            Assert.That(plainOutput[0], Is.EqualTo(12f + 1f).Within(1e-5f), "Plain static abstract");
            Assert.That(plainOutput[1], Is.EqualTo(4f).Within(1e-5f), "Plain instance member");
            Assert.That(plainOutput[2], Is.EqualTo(0f).Within(1e-5f), "Plain static virtual default");
        });
    }

    private static float[] Run<TMaterial>(string slang, TMaterial material) where TMaterial : unmanaged
    {
        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(slang);

        var output = new float[3];
        using var buffers = new PinnedBuffers();
        var push = new Push<TMaterial> { Materials = buffers.Add(new[] { material }), Output = buffers.Add(output) };
        shader.Dispatch(1, 1, 1, buffers.Heap, ref push);

        return output;
    }
}
