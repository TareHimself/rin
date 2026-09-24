namespace Rin.Slang.Tests;

public class ShaderPackageTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("rin-slang-package-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_root, true);
    }

    private CompiledShader MakeShader()
    {
        var source = Path.Combine(_root, "shader.slang");
        File.WriteAllText(source, "void main() {}");
        return new CompiledShader
        {
            Kind = ShaderKind.Compute,
            ThreadGroupSize = [8, 4, 1],
            Stages =
            [
                new CompiledStage
                {
                    Stage = "compute",
                    Spirv = [1, 2, 3, 4],
                    Reflection = new SlangReflectionData()
                }
            ],
            Dependencies = [("shader.slang", source)]
        };
    }

    [Test]
    public void ManifestRecordsTheSourceHashOfTheDependencies()
    {
        var shader = MakeShader();
        var path = Path.Combine(_root, "shader.crsh");

        ShaderPackageWriter.WriteToFile(shader, path);
        var manifest = ShaderPackageReader.ReadManifestFromFile(path);

        Assert.That(manifest, Is.Not.Null);
        Assert.That(manifest!.SourceHash, Is.EqualTo(ShaderSourceHash.Compute(shader.Dependencies)));
    }

    [Test]
    public void PackageRoundTripsKindStagesAndThreadGroupSize()
    {
        var shader = MakeShader();
        var path = Path.Combine(_root, "shader.crsh");

        ShaderPackageWriter.WriteToFile(shader, path);
        var read = ShaderPackageReader.ReadFromFile(path);

        Assert.That(read.Kind, Is.EqualTo(ShaderKind.Compute));
        Assert.That(read.ThreadGroupSize, Is.EqualTo(new uint[] { 8, 4, 1 }));
        Assert.That(read.Stages, Has.Length.EqualTo(1));
        Assert.That(read.Stages[0].Stage, Is.EqualTo("compute"));
        Assert.That(read.Stages[0].Spirv, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }
}
