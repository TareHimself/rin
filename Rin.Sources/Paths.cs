namespace Rin.Sources;

public static class Paths
{
    public static string Normalize(string path) => path.Contains('\\') ? path.Replace('\\', '/') : path;
}