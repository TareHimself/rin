// See https://aka.ms/new-console-template for more information

using misc.StrokeExpansion;
using Rin.Core;

Global.Get().Sources.AddSource(AssemblyContentResource.New<MainModule>("StrokeExpansion", string.Empty));
Global.Get().Run();