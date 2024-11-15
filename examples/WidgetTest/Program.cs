// See https://aka.ms/new-console-template for more information

using rin.Core;
using rin.Core.Logging;
using rsl;using rsl.Generator;
using rsl.Nodes;

// var tokens = Tokenizer.Run("<>", @"for (float i = 0.0; i < 4.0; i++) {
//             uv = fract(uv * 1.5) - 0.5;
//
//             float d = length(uv) * exp(length(uv0) * -1.0);
//
//             float3 col = palette(length(uv0) + i*0.4 + iTime*0.4);
//
//             d = sin(d*8.0 + iTime)/8.0;
//             d = abs(d);
//
//             d = pow(0.01 / d, 1.2);
//
//             finalColor += col * d;
//         }");
// var ast = Parser.ParseFor(ref tokens);
// var tokens = Tokenizer.Run(@"C:\Github\aerox\rin.Scene\resources\shaders\scene\clusters.rsl");
// var ast = Parser.Parse(ref tokens);
// ast.ResolveStructReferences();
// ast = ast.ExtractScope(ScopeType.Compute);
// ast = ast.ExtractFunctionWithDependencies("main")!;
// var data = new GlslGenerator().Run(ast.Statements);
//Console.WriteLine("TO");
SRuntime.Get().Run();
