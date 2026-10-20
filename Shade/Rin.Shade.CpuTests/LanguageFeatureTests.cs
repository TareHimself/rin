using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Shade.Tests;
using Rin.Shade.Transpiler;
using Rin.Slang.Compiler;

namespace Rin.Shade.CpuTests;

/// <summary>
///     Compiles static readonly constants, switch expressions, extension members and generic methods with the
///     real Slang compiler and checks the numbers, since the emitter tests only check the text.
/// </summary>
public class LanguageFeatureTests
{
    private const string Source = """
        using System.Numerics;
        using Rin.Shade;

        namespace FeatureCpuCheck;

        public static class Lighting
        {
            public static readonly Vector3 LightDirection = new Vector3(0f, 1f, 0f);
            public static readonly float Ambient = 0.25f;
            public static readonly float[] Weights = { 1f, 2f, 3f };
        }

        public struct Kernel
        {
            public static readonly float Scale = 0.5f;
            public float Bias;
        }

        public static class VectorExtras
        {
            extension(in Vector3 v)
            {
                public float Sum() => v.X + v.Y + v.Z;

                public Vector3 Scaled(float by) => v * by;
            }

            extension(Vector3)
            {
                public static Vector3 Up() => new Vector3(0f, 1f, 0f);
            }

            public static Vector3 WithX(this Vector3 v, float x)
            {
                v.X = x;
                return v;
            }

            public static void Bump(ref this Vector3 v, float by)
            {
                v.Y += by;
            }
        }

        public static class Helpers
        {
            public static T Pick<T>(bool first, T a, T b) where T : unmanaged
            {
                T chosen = first ? a : b;
                return chosen;
            }

            public static float Band(float distance) => distance switch
            {
                < 0.25f => 0f,
                >= 0.25f and < 0.75f => 0.5f,
                _ => 1f
            };
        }

        public struct Push
        {
            public Kernel Kernel;
            public Vector3 Normal;
            public float Distance;
            public int Flag;
            public BufferRef<float> Output;
        }

        [Shader]
        public class FeatureShader : Shader
        {
            [Push] protected Push Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                var sum = 0f;
                for (var i = 0; i < Lighting.Weights.Length; i++) sum += Lighting.Weights[i];
                Push.Output[0] = Lighting.Ambient + Max(Dot(Push.Normal, Lighting.LightDirection), 0f) + sum * Kernel.Scale + Push.Kernel.Bias;

                Push.Output[1] = Helpers.Band(Push.Distance) + (Push.Flag switch { 0 => 10f, 1 => 20f, _ => 30f });

                var v = Push.Normal.Scaled(2f);
                v.Bump(1f);
                var moved = v.WithX(7f);
                Push.Output[2] = v.Sum() + moved.X + Vector3.Up().Y;

                Push.Output[3] = Helpers.Pick(Push.Flag != 0, Push.Distance, -1f) + Helpers.Pick(Push.Flag == 0, Push.Normal, Vector3.Up()).X;
            }
        }
        """;

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct Push
    {
        public float KernelBias;
        public Vector3 Normal;
        public float Distance;
        public int Flag;
        public float* Output;
    }

    [Test]
    public unsafe void ConstantsSwitchExpressionsExtensionsAndGenericsRunOnTheCpu()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);

        using var compiler = new HostComputeCompiler();
        using var shader = compiler.Compile(result.Shaders["FeatureShader"]);

        var output = new float[4];
        fixed (float* outputPointer = output)
        {
            var push = new Push
            {
                KernelBias = 0.125f,
                Normal = new Vector3(0.5f, 0.75f, 0.25f),
                Distance = 0.5f,
                Flag = 1,
                Output = outputPointer
            };
            shader.Dispatch(1, 1, 1, ref push);
        }

        Assert.Multiple(() =>
        {
            Assert.That(output[0], Is.EqualTo(0.25f + 0.75f + 6f * 0.5f + 0.125f).Within(1e-5f), "static readonly constants");
            Assert.That(output[1], Is.EqualTo(0.5f + 20f).Within(1e-5f), "switch expressions");
            Assert.That(output[2], Is.EqualTo(1f + 2.5f + 0.5f + 7f + 1f).Within(1e-5f), "extension members");
            Assert.That(output[3], Is.EqualTo(0.5f + 0f).Within(1e-5f), "generic methods");
        });
    }
}
