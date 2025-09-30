namespace Rin.Framework.Sources;

public static class Paths
{
    public static string Normalize(string path)
    {
        return path.Contains('\\') ? path.Replace('\\', '/') : path;
    }
}