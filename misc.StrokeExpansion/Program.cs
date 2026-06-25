// See https://aka.ms/new-console-template for more information

using misc.StrokeExpansion;
using Rin.Core;

SFramework.Get().Sources.AddSource(AssemblyContentResource.New<MainModule>("StrokeExpansion", string.Empty));
SFramework.Get().Run();