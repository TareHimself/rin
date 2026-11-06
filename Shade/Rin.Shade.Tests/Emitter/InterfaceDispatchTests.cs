using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class InterfaceDispatchTests
{
    [Test]
    public void StaticAbstractCallThroughClosedGenericBaseResolvesToTheImplementation()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace InterfaceDispatchCheck;

                               public struct Surface
                               {
                                   public Vector3 Color;
                                   public float Roughness;
                               }

                               public interface ISurfaceMaterial<TSelf> where TSelf : unmanaged, ISurfaceMaterial<TSelf>
                               {
                                   static abstract Surface Evaluate(in TSelf material, Vector2 uv);
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
                               }

                               public struct SurfacePush<TMaterial> where TMaterial : unmanaged
                               {
                                   public BufferRef<TMaterial> Materials;
                                   public BufferRef<Surface> Output;
                               }

                               public abstract class SurfaceShader<TMaterial> : Shader
                                   where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
                               {
                                   [Push] protected SurfacePush<TMaterial> Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = TMaterial.Evaluate(Push.Materials[0], new Vector2(0.5f));
                                   }
                               }

                               [Shader]
                               public class GoldShader : SurfaceShader<Gold>;
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["GoldShader"]);
    }

    [Test]
    public void InstanceCallOnConstrainedStructResolvesToTheImplementation()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace InterfaceDispatchCheck;

                               public struct Surface
                               {
                                   public Vector3 Color;
                                   public float Roughness;
                               }

                               public interface ISurfaceMaterial
                               {
                                   Surface Evaluate(Vector2 uv);
                               }

                               public struct Gold : ISurfaceMaterial
                               {
                                   public Vector3 Tint;
                                   public float Roughness;

                                   public Surface Evaluate(Vector2 uv)
                                   {
                                       Surface surface;
                                       surface.Color = Tint * uv.X;
                                       surface.Roughness = Roughness;
                                       return surface;
                                   }
                               }

                               public struct SurfacePush<TMaterial> where TMaterial : unmanaged
                               {
                                   public BufferRef<TMaterial> Materials;
                                   public BufferRef<Surface> Output;
                               }

                               public abstract class SurfaceShader<TMaterial> : Shader
                                   where TMaterial : unmanaged, ISurfaceMaterial
                               {
                                   [Push] protected SurfacePush<TMaterial> Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var material = Push.Materials[0];
                                       Push.Output[0] = material.Evaluate(new Vector2(0.5f));
                                   }
                               }

                               [Shader]
                               public class GoldShader : SurfaceShader<Gold>;
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["GoldShader"]);
    }

    [Test]
    public void StaticVirtualDefaultIsUsedWhenTheStructDoesNotOverrideIt()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace InterfaceDispatchCheck;

                               public interface IVertexOffset<TSelf> where TSelf : unmanaged, IVertexOffset<TSelf>
                               {
                                   static virtual Vector3 Offset(in TSelf material, Vector3 world) => new Vector3(0f);
                               }

                               public struct Still : IVertexOffset<Still>
                               {
                                   public float Unused;
                               }

                               public struct Wavy : IVertexOffset<Wavy>
                               {
                                   public float Amplitude;

                                   public static Vector3 Offset(in Wavy material, Vector3 world) =>
                                       new Vector3(0f, material.Amplitude * world.X, 0f);
                               }

                               public struct OffsetPush<TMaterial> where TMaterial : unmanaged
                               {
                                   public BufferRef<TMaterial> Materials;
                                   public BufferRef<Vector3> Output;
                               }

                               public abstract class OffsetShader<TMaterial> : Shader
                                   where TMaterial : unmanaged, IVertexOffset<TMaterial>
                               {
                                   [Push] protected OffsetPush<TMaterial> Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       var world = new Vector3(1f, 2f, 3f);
                                       Push.Output[0] = world + TMaterial.Offset(Push.Materials[0], world);
                                   }
                               }

                               [Shader]
                               public class StillShader : OffsetShader<Still>;

                               [Shader]
                               public class WavyShader : OffsetShader<Wavy>;
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["StillShader"], "Still");
        Snapshot.Verify(result.Shaders["WavyShader"], "Wavy");
    }

    [Test]
    public void ConstrainedCallInsideAGenericMethodResolvesPerInstantiation()
    {
        const string source = """
                               using System.Numerics;
                               using Rin.Shade;

                               namespace InterfaceDispatchCheck;

                               public interface IScale<TSelf> where TSelf : unmanaged, IScale<TSelf>
                               {
                                   static abstract float Scale(in TSelf value);
                               }

                               public struct Half : IScale<Half>
                               {
                                   public float Value;
                                   public static float Scale(in Half value) => value.Value * 0.5f;
                               }

                               public struct Double : IScale<Double>
                               {
                                   public float Value;
                                   public static float Scale(in Double value) => value.Value * 2f;
                               }

                               public static class Scaling
                               {
                                   public static float Apply<T>(in T value) where T : unmanaged, IScale<T> => T.Scale(value);
                               }

                               public struct ScalePush
                               {
                                   public Half A;
                                   public Double B;
                                   public BufferRef<float> Output;
                               }

                               [Shader]
                               public class ScaleShader : Shader
                               {
                                   [Push] protected ScalePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Scaling.Apply(Push.A) + Scaling.Apply(Push.B);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["ScaleShader"]);
    }

    [Test]
    public void CallThroughAnInterfaceTypedValueIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace InterfaceDispatchCheck;

                               public interface IScale
                               {
                                   float Scale();
                               }

                               public struct Half : IScale
                               {
                                   public float Value;
                                   public float Scale() => Value * 0.5f;
                               }

                               public struct ScalePush
                               {
                                   public Half A;
                                   public BufferRef<float> Output;
                               }

                               public static class Scaling
                               {
                                   public static float Apply(IScale value) => value.Scale();
                               }

                               [Shader]
                               public class ScaleShader : Shader
                               {
                                   [Push] protected ScalePush Push;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Push.Output[0] = Scaling.Apply(Push.A);
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Select(d => d.Id),
            Does.Contain(Diagnostics.Emitter.InterfaceCallWithoutConcreteType.Id));
    }
}
