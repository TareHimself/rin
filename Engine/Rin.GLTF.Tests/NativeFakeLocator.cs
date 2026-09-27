namespace Rin.GLTF.Tests;

internal static class NativeFakeLocator
{
    public static string Find(string fakeProjectName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "rin.sln")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException($"Could not find rin.sln above {AppContext.BaseDirectory}");

        var fakeDir = Path.Combine(dir.FullName, "native", "Fakes", fakeProjectName);
        var match = Directory.EnumerateFiles(fakeDir, $"{fakeProjectName}.dll", SearchOption.AllDirectories)
            .FirstOrDefault(p => p.Split(Path.DirectorySeparatorChar).Contains("publish"));
        if (match is null)
            throw new InvalidOperationException(
                $"{fakeProjectName}.dll not published - run scripts/publish_native_fakes.py first");

        return match;
    }
}
