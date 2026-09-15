using experiment.Slug;
using Rin.Core;
using Rin.Core.Sources;

Global.Sources.AddSource(AssemblyContentResource.New<SlugApplication>("Slug"));

using var app = new SlugApplication();
app.Run();
