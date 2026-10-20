using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class GroupSharedTests
{
    [Test]
    public void GroupSharedFieldsLowerToModuleScopeDeclarations()
    {
        const string source = """
                               using System.Runtime.CompilerServices;
                               using Rin.Shade;

                               namespace GroupSharedCheck;

                               [InlineArray(64)]
                               public struct SharedFloats
                               {
                                   private float _element;
                               }

                               [Shader]
                               public class GroupSharedShader : Shader
                               {
                                   public struct ReducePush
                                   {
                                       public BufferRef<float> Input;
                                       public BufferRef<float> Output;
                                   }

                                   public struct ComputeIn
                                   {
                                       [DispatchThreadId] public uint ThreadId;
                                       [GroupIndex] public int GroupIndex;
                                       [GroupId] public uint GroupId;
                                   }

                                   [Push] protected ReducePush Push;

                                   [GroupShared] private static SharedFloats Partial;
                                   [GroupShared] private static uint Count;

                                   [Compute(64, 1, 1)]
                                   public void Compute(ComputeIn input)
                                   {
                                       Partial[input.GroupIndex] = Push.Input[input.ThreadId];
                                       if (input.GroupIndex == 0) Count = 0u;
                                       GroupMemoryBarrierWithGroupSync();

                                       InterlockedAdd(ref Count, 1u);
                                       GroupMemoryBarrierWithGroupSync();

                                       if (input.GroupIndex != 0) return;

                                       var sum = 0f;
                                       for (var i = 0; i < (int)Count; i++) sum += Partial[i];
                                       Push.Output[input.GroupId] = sum;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics, Is.Empty);

        Snapshot.Verify(result.Shaders["GroupSharedShader"]);
    }

    [Test]
    public void InstanceGroupSharedFieldIsRejected()
    {
        const string source = """
                               using Rin.Shade;

                               namespace GroupSharedInstanceCheck;

                               [Shader]
                               public class GroupSharedInstanceShader : Shader
                               {
                                   public struct Push
                                   {
                                       public BufferRef<uint> Output;
                                   }

                                   [Push] protected Push Data;

                                   [GroupShared] private uint Count;

                                   [Compute(1, 1, 1)]
                                   public void Compute()
                                   {
                                       Data.Output[0] = 1u;
                                   }
                               }
                               """;

        var result = ShadeEmitter.Emit(CompilationBuilder.Build(source));

        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.UnsupportedGroupSharedField.Id), Is.True);
    }
}
