using experiment.FontIcon;
using Rin.Core;
using Rin.Core.Sources;

Global.Sources.AddSource(AssemblyContentResource.New<FontIconApplication>("FontIcon"));

using var app = new FontIconApplication();
app.Run();
