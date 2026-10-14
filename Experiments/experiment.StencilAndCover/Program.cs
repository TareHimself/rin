using experiment.StencilAndCover;
using Rin.Core;
using Rin.Core.Sources;

Global.Sources.AddSource(AssemblyContentResource.New<StencilAndCoverApplication>("StencilAndCover"));

using var app = new StencilAndCoverApplication();
app.Run();
