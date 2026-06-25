// using System.Linq;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Rin.Core.Generators;
// using Xunit;
//
// namespace Rin.SourceGenerators.Tests;
//
// public class wSourceGeneratorWithAttributesTests
// {
//     private const string VectorClassText = @"
// using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;
//
// namespace Rin.Core.Audio.Effects;
//
// [AudioEffect]
// public partial struct TestEffect
// {
//     public static void Process(ReadOnlySpan<float> input, Span<float> output)
//     {
//         for (var i = 0; i < input.Length; i++)
//         {
//             output[i] = input[i];
//             
//         }
//     }
// }
// ";
//     
//
//     [Fact]
//     public void GenerateReportMethod()
//     {
//         // Create an instance of the source generator.
//         var generator = new AudioEffectGenerator();
//
//         // Source generators should be tested using 'GeneratorDriver'.
//         var driver = CSharpGeneratorDriver.Create(generator);
//
//         // We need to create a compilation with the required source code.
//         var compilation = CSharpCompilation.Create(nameof(SourceGeneratorWithAdditionalFilesTests),
//             new[] { CSharpSyntaxTree.ParseText(VectorClassText) },
//             new[]
//             {
//                 // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
//                 MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
//             });
//
//         // Run generators and retrieve all results.
//         var runResult = driver.RunGenerators(compilation).GetRunResult();
//
//         // All generated files can be found in 'RunResults.GeneratedTrees'.
//         var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("Vector3.g.cs"));
//
//         // // Complex generators should be tested using text comparison.
//         // Assert.Equal(ExpectedGeneratedClassText, generatedFileSyntax.GetText().ToString(),
//         //     ignoreLineEndingDifferences: true);
//     }
// }