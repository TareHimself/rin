namespace Rin.Graphics.Vulkan.Tests;

internal static class NativeFakeLocator
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "rin.sln")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException($"Could not find rin.sln above {AppContext.BaseDirectory}");
        return dir.FullName;
    }

    public static string Find(string fakeProjectName)
    {
        var fakeDir = Path.Combine(FindRepoRoot(), "native", "Fakes", fakeProjectName);
        var match = Directory.EnumerateFiles(fakeDir, $"{fakeProjectName}.dll", SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Split(Path.DirectorySeparatorChar).Contains("publish"));
        if (match is null)
            throw new InvalidOperationException(
                $"{fakeProjectName}.dll not published - run scripts/publish_native_fakes.py first");

        return match;
    }
}
