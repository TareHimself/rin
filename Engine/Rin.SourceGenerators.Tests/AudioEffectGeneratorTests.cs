using System.Linq;
using Microsoft.CodeAnalysis;
using Rin.Core.Audio.Effects;
using Xunit;

namespace Rin.SourceGenerators.Tests;

public class AudioEffectGeneratorTests
{
    private const string Usings = @"
using System;
using Rin.Core.Audio.Effects;

namespace TestNamespace;

public struct Settings { public float Gain; }
public struct Memory { public float Last; }
";

    private static GeneratorDriverRunResult Run(string effect) =>
        GeneratorHarness.Run<AudioEffectGenerator>(Usings + effect, typeof(AudioEffectContext));

    private static string GeneratedEffect(GeneratorDriverRunResult runResult) =>
        runResult.GeneratedTrees.Single().GetText().ToString();

    [Fact]
    public void GeneratesADescriptorForAnEffectWithOnlyInputAndOutput()
    {
        var runResult = Run(@"
[AudioEffect]
public partial struct TestEffect
{
    public static void Process(ReadOnlySpan<float> input, Span<float> output) { }
}
");

        Assert.Empty(runResult.Diagnostics);

        var generatedText = GeneratedEffect(runResult);

        Assert.Contains("internal class EffectDescriptor", generatedText);
        Assert.Contains("Descriptor = new EffectDescriptor();", generatedText);
        Assert.Contains("NoAudioEffectParameters", generatedText);
        Assert.DoesNotContain("inState", generatedText);
    }

    [Fact]
    public void GeneratesStateParametersAndContextAccessForTheArgumentsProcessTakes()
    {
        var runResult = Run(@"
[AudioEffect]
public partial struct TestEffect
{
    public static void Process(in AudioEffectContext ctx, ReadOnlySpan<float> input, Span<float> output,
        ref Memory state, in Settings parameters) { }
}
");

        Assert.Empty(runResult.Diagnostics);

        var generatedText = GeneratedEffect(runResult);

        Assert.Contains("Unsafe.AsRef<global::Rin.Core.Audio.Effects.AudioEffectContext>(ctx);", generatedText);
        Assert.Contains("ref var inState = ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::TestNamespace.Memory>(state);",
            generatedText);
        Assert.Contains("ParameterSlots<global::TestNamespace.Settings>.Read(parameters);", generatedText);
        Assert.Contains("IAudioEffectDescriptor<global::TestNamespace.Settings>", generatedText);
    }

    [Theory]
    [InlineData("RIN00001", "struct", "public static void Process(ReadOnlySpan<float> input, Span<float> output) { }")]
    [InlineData("RIN00002", "partial struct", "")]
    [InlineData("RIN00011", "partial struct",
        "public static void Process(ReadOnlySpan<float> input, Span<float> output) { } public static void Process(ReadOnlySpan<float> input) { }")]
    [InlineData("RIN00009", "partial struct", "public void Process(ReadOnlySpan<float> input, Span<float> output) { }")]
    [InlineData("RIN00006", "partial struct",
        "public static void Process(string ctx, ReadOnlySpan<float> input, Span<float> output) { }")]
    [InlineData("RIN00007", "partial struct",
        "public static void Process(ReadOnlySpan<float> input, Span<float> output, ref string state) { }")]
    [InlineData("RIN00008", "partial struct",
        "public static void Process(ReadOnlySpan<float> input, Span<float> output, Memory state) { }")]
    [InlineData("RIN00008", "partial struct",
        "public static void Process(ReadOnlySpan<float> input, Span<float> output, Settings parameters) { }")]
    [InlineData("RIN00003", "partial struct", "public static void Process(Span<float> input, Span<float> output) { }")]
    [InlineData("RIN00004", "partial struct",
        "public static void Process(ReadOnlySpan<float> input, ReadOnlySpan<float> output) { }")]
    [InlineData("RIN00005", "partial struct",
        "public static void Process(ReadOnlySpan<float> input, Span<float> output, int volume) { }")]
    [InlineData("RIN00010", "partial struct", "public static void Process(ReadOnlySpan<float> input) { }")]
    public void InvalidEffectIsRejected(string expectedDiagnostic, string typeKeywords, string members)
    {
        var runResult = Run($"[AudioEffect] public {typeKeywords} TestEffect {{ {members} }}");

        Assert.Contains(runResult.Diagnostics, d => d.Id == expectedDiagnostic);
        Assert.Empty(runResult.GeneratedTrees);
    }
}
