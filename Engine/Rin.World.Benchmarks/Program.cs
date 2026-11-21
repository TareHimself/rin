using BenchmarkDotNet.Running;
using Rin.World.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Position).Assembly).Run(args);
