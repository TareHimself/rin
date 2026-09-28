namespace SampleStdlib;

// Deliberately NOT listed in SampleStdlib.csproj's <EmbeddedResource> items - compiles into
// SampleStdlib.dll normally, but has no embedded source for a downstream scratch compilation to
// pull back out. Used to verify what happens when Sd.cs calls into a sibling file that wasn't
// marked for embedding.
public static class MathHelpers
{
    public static float Square(float x) => x * x;
}
