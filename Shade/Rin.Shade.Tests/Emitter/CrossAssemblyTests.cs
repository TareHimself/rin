using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Rin.Shade.Tests.Emitter;

using Rin.Shade.Transpiler;

/// <summary>
/// Proves the design doc's cross-assembly inheritance mechanism against real, separately-built
/// sample projects (Shade/Samples/) rather than a single-compilation fixture: SampleGame's
/// DerivedMeshShader derives from SampleWorld's BaseMeshShader (a different assembly), whose own
/// Compute body calls into SampleStdlib's Sd.SdCircle (a third assembly) - none of that body text
/// exists as source anywhere in SampleGame's own compilation, only as embedded resources in the
/// referenced DLLs. Build the sample projects first (`dotnet build Shade/Samples/SampleGame`)
/// before running this - it reads their actual output DLLs, not fixture source.
/// </summary>
public class CrossAssemblyTests
{
    private static string SamplesRoot([CallerFilePath] string callerPath = "") =>
        Path.Combine(Path.GetDirectoryName(callerPath)!, "..", "..", "Samples");

    private static string SampleDll(string project, string dll) =>
        Path.Combine(SamplesRoot(), project, "bin", "Debug", "net10.0", dll);

    private static string SampleSource(string project, string fileName) =>
        Path.Combine(SamplesRoot(), project, fileName);

    [Test]
    public void DerivedShaderFlattensCrossAssemblyBaseAndTransitiveStdlibCall()
    {
        var sampleStdlibDll = SampleDll("SampleStdlib", "SampleStdlib.dll");
        var sampleWorldDll = SampleDll("SampleWorld", "SampleWorld.dll");

        Assert.That(File.Exists(sampleWorldDll), Is.True,
            $"'{sampleWorldDll}' not found - build Shade/Samples/SampleGame first");

        var derivedSource = File.ReadAllText(SampleSource("SampleGame", "DerivedMeshShader.cs"));

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Shader).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(sampleStdlibDll))
            .Append(MetadataReference.CreateFromFile(sampleWorldDll));

        var scratch = ScratchCompilationBuilder.Build([derivedSource], references);
        var result = ShadeEmitter.Emit(scratch.Compilation, scratch.LocalTrees);

        Assert.That(result.Diagnostics, Is.Empty);
        var slang = result.Shaders["DerivedMeshShader"];

        // The push constants struct is declared on the base (SampleWorld), never in SampleGame's own
        // source - reachable only via SampleWorld's embedded resource.
        Assert.That(slang, Does.Contain("struct MeshPushConstants"));

        // sdCircle is declared in SampleStdlib - a THIRD assembly, never directly referenced by
        // SampleGame's own source, reachable only transitively through SampleWorld's embedded call.
        Assert.That(slang, Does.Contain("float sdCircle(float2 location, float2 center, float r)"));

        // The derived override's own body, calling both the base's helper and the stdlib directly.
        Assert.That(slang,
            Does.Contain("push.output[0] = sdCircle(push.location, push.center, push.radius) + dot2(push.location);"));
    }

    [Test]
    public void CallingIntoAFileNotMarkedForEmbeddingProducesADiagnosticNotWrongOutput()
    {
        // SampleStdlib's Sd.SdCircleSquared calls MathHelpers.Square, which lives in
        // NotEmbedded.cs - deliberately never added to SampleStdlib.csproj's <EmbeddedResource>
        // items, even though it compiles into SampleStdlib.dll normally. Proves the walk doesn't
        // silently drop the call or crash - "no source available, add a [SlangCall] binding, or
        // mark its file ShaderCompile" is exactly the failure mode the design doc describes.
        const string localSource = """
                                    using System.Numerics;
                                    using Rin.Shade;
                                    using SampleStdlib;

                                    namespace NotEmbeddedCheck;

                                    public struct ProbePush
                                    {
                                        public Vector2 Location;
                                        public Vector2 Center;
                                        public float Radius;
                                        public BufferRef<float> Output;
                                    }

                                    [Shader("Fixtures/probe.slang")]
                                    public class ProbeShader : Shader
                                    {
                                        [Push] protected ProbePush Push;

                                        [Compute(1, 1, 1)]
                                        public void Compute()
                                        {
                                            Push.Output[0] = Sd.SdCircleSquared(Push.Location, Push.Center, Push.Radius);
                                        }
                                    }
                                    """;

        var sampleStdlibDll = SampleDll("SampleStdlib", "SampleStdlib.dll");
        Assert.That(File.Exists(sampleStdlibDll), Is.True,
            $"'{sampleStdlibDll}' not found - build Shade/Samples/SampleStdlib first");

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Shader).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(sampleStdlibDll));

        var scratch = ScratchCompilationBuilder.Build([localSource], references);
        var result = ShadeEmitter.Emit(scratch.Compilation, scratch.LocalTrees);

        // sdCircleSquared itself IS reachable (it's in the embedded Sd.cs), and gets walked and
        // emitted - only the call to the non-embedded MathHelpers.Square fails, cleanly.
        var slang = result.Shaders["ProbeShader"];
        Assert.That(slang, Does.Contain("float sdCircleSquared("));
        Assert.That(result.Diagnostics.Any(d => d.Id == Diagnostics.Emitter.NoSourceForMethod.Id), Is.True);
    }
}
