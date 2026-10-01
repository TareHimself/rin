// See https://aka.ms/new-console-template for more information

using Rin.Core;
using Rin.Core.Sources;
using ViewsTest;

Global.Sources.AddSource(AssemblyContentResource.New<ViewsTestApplication>("ViewsTest"));
Global.Sources.AddSource(AssemblyContentResource.New<ViewsTestApplication>("Shaders/ViewsTest"));

using var app = new ViewsTestApplication { StencilTest = args.Contains("--stencil") };
app.Run();