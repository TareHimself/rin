namespace Rin.Framework.Sources;

public class FileSystemSource : ISource
{
    public string BasePath => "fs";

    public Stream Read(string path)
    {
        return File.OpenRead(ToSystemPath(path));
    }

    public Stream Write(string path)
    {
        return File.OpenWrite(path);
    }

    private string ToSystemPath(string path)
    {
        var pruned = path[BasePath.Length..];

        if (OperatingSystem.IsWindows()) pruned = pruned[1..].Replace('/', '\\');

        return pruned;
    }
}