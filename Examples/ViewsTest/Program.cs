// See https://aka.ms/new-console-template for more information

using Rin.Core;
using Rin.Core.Sources;
using ViewsTest;

Global.Sources.AddSource(AssemblyContentResource.New<ViewsTestApplication>("ViewsTest"));

using var app = new ViewsTestApplication();
app.Run();