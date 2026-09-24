namespace Rin.Slang.Compiler.Tests;

public class ShaderCompilerTests
{
    private const string ComputeShader = """
                                         #include "common.slang"

                                         [shader("compute")]
                                         [numthreads(1, 1, 1)]
                                         void compute(uint3 id: SV_DispatchThreadID)
                                         {
                                         }
                                         """;

    private ShaderCompiler _compiler = null!;
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("rin-slang-compiler-").FullName;
        var options = new ShaderCompilerOptions();
        options.AddSearchPath(_root);
        options.SetPortableRoot(_root);
        _compiler = new ShaderCompiler(options);
    }

    [TearDown]
    public void TearDown()
    {
        _compiler.Dispose();
        Directory.Delete(_root, true);
    }

    private string WriteSource(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private string CompileToPackage(string sourcePath)
    {
        var packagePath = Path.ChangeExtension(sourcePath, ".crsh");
        Assert.That(_compiler.TryCompile(sourcePath, out var shader), Is.True);
        ShaderPackageWriter.WriteToFile(shader!, packagePath);
        return packagePath;
    }

    [Test]
    public void ResolvesTransitiveIncludesAsPortableIds()
    {
        WriteSource("lib/inner.slang", "static const int Inner = 1;");
        WriteSource("common.slang", "#include \"lib/inner.slang\"");
        var source = WriteSource("shaders/main.slang", ComputeShader);

        var ids = _compiler.ResolveDependencies(source).Select(d => d.PortableId);

        Assert.That(ids, Is.EquivalentTo(new[] { "common.slang", "lib/inner.slang", "shaders/main.slang" }));
    }

    [Test]
    public void FreshPackageIsUpToDate()
    {
        WriteSource("common.slang", "static const int Value = 1;");
        var source = WriteSource("main.slang", ComputeShader);

        Assert.That(_compiler.IsUpToDate(CompileToPackage(source), source), Is.True);
    }

    [Test]
    public void EditingAnIncludeMakesThePackageStale()
    {
        var common = WriteSource("common.slang", "static const int Value = 1;");
        var source = WriteSource("main.slang", ComputeShader);
        var package = CompileToPackage(source);

        File.WriteAllText(common, "static const int Value = 2;");

        Assert.That(_compiler.IsUpToDate(package, source), Is.False);
    }

    [Test]
    public void EditingTheSourceMakesThePackageStale()
    {
        WriteSource("common.slang", "static const int Value = 1;");
        var source = WriteSource("main.slang", ComputeShader);
        var package = CompileToPackage(source);

        File.AppendAllText(source, "\n// edited");

        Assert.That(_compiler.IsUpToDate(package, source), Is.False);
    }

    [Test]
    public void UnrelatedFilesDoNotAffectFreshness()
    {
        WriteSource("common.slang", "static const int Value = 1;");
        var unrelated = WriteSource("unrelated.slang", "static const int Other = 1;");
        var source = WriteSource("main.slang", ComputeShader);
        var package = CompileToPackage(source);

        File.WriteAllText(unrelated, "static const int Other = 2;");

        Assert.That(_compiler.IsUpToDate(package, source), Is.True);
    }

    [Test]
    public void MissingPackageIsStale()
    {
        WriteSource("common.slang", "static const int Value = 1;");
        var source = WriteSource("main.slang", ComputeShader);

        Assert.That(_compiler.IsUpToDate(Path.Combine(_root, "missing.crsh"), source), Is.False);
    }

    [Test]
    public void CorruptPackageIsStale()
    {
        WriteSource("common.slang", "static const int Value = 1;");
        var source = WriteSource("main.slang", ComputeShader);
        var package = WriteSource("main.crsh", "not a tar archive");

        Assert.That(_compiler.IsUpToDate(package, source), Is.False);
    }
}
