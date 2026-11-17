namespace Rin.TestSupport;

/// <summary>
///     Finds files in the checkout from a test's output folder. Linked into the test projects that need it.
/// </summary>
internal static class RepoFiles
{
    public static string Root { get; } = FindRoot();

    public static string NativeSource(string nativeProjectName, string file) =>
        File.ReadAllText(Path.Combine(Root, "native", nativeProjectName, "src", file));

    public static string NativeFake(string fakeProjectName)
    {
        var fakeDirectory = Path.Combine(Root, "native", "Fakes", fakeProjectName);
        var match = Directory.EnumerateFiles(fakeDirectory, $"{fakeProjectName}.dll", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.Split(Path.DirectorySeparatorChar).Contains("publish"));
        return match ?? throw new InvalidOperationException(
            $"{fakeProjectName}.dll not published - run scripts/publish_native_fakes.py first");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "rin.sln")))
            directory = directory.Parent;
        return directory?.FullName ??
               throw new InvalidOperationException($"Could not find rin.sln above {AppContext.BaseDirectory}");
    }
}
