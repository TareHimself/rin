namespace Rin.World.Tests;

// Throwaway reference so Rin.Slang.Discovery's syntax-only scan (ShaderReferenceScanner) finds the
// Rin.Shade-generated Shaders/World/Mesh/Compute/generated/bounds_update.slang, proving the
// automated Rin.Shade.MSBuild -> Rin.Slang.MSBuild pipeline end to end (Step A of the rollout,
// see BoundsUpdateShader.cs) without touching BoundsUpdatePass.cs's real reference. Never invoked -
// the scanner matches purely on invocation syntax, not on what MakeCompute actually resolves to.
// Delete this once Step B cuts BoundsUpdatePass.cs over to the canonical path.
internal static class ShadeIntegrationProbe
{
    private static void Probe()
    {
        MakeCompute("Shaders/World/Mesh/Compute/generated/bounds_update.slang");
    }

    private static void MakeCompute(string path)
    {
    }
}
