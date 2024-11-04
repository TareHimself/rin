// See https://aka.ms/new-console-template for more information

using rin.Core;
using rsl;

var tokens = Tokenizer.Run("<test>","""
                                    finalColor /= (Z*Z);
                                    """);
var ast = Parser.ParseExpression(ref tokens);
Console.WriteLine("TO");
SRuntime.Get().Run();


Console.WriteLine("Hello, World!");