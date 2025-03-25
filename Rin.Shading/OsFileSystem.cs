namespace Rin.Shading;

public class OsFileSystem : IFileSystem
{
    public string GetUniqueIdentifier(string fullSourcePath)
    {
        return fullSourcePath;
    }

    public string GetFullSourcePath(string path)
    {
        return Path.GetFullPath(path);
    }

    public string GetFullIncludePath(string fullSourcePath, string includePath)
    {

        if (!Path.IsPathRooted(includePath))
        {
            return Path.GetFullPath(includePath,fullSourcePath);
        }
        return Path.GetFullPath(fullSourcePath);
    }

    public Stream GetContent(string fullSourcePath)
    {
        return File.OpenRead(fullSourcePath);
    }
}