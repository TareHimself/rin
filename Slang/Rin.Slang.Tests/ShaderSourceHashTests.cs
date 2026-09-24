namespace Rin.Slang.Tests;

public class ShaderSourceHashTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("rin-slang-hash-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_root, true);
    }

    private (string PortableId, string AbsolutePath) Dependency(string portableId, string content)
    {
        var path = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        File.WriteAllText(path, content);
        return (portableId, path);
    }

    [Test]
    public void SameContentAndIdsHashTheSame()
    {
        var a = ShaderSourceHash.Compute([Dependency("a.slang", "void main() {}")]);
        var b = ShaderSourceHash.Compute([Dependency("a.slang", "void main() {}")]);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void ChangedContentChangesTheHash()
    {
        var before = ShaderSourceHash.Compute([Dependency("a.slang", "void main() {}")]);
        var after = ShaderSourceHash.Compute([Dependency("a.slang", "void main() { }")]);

        Assert.That(after, Is.Not.EqualTo(before));
    }

    [Test]
    public void RenamedDependencyChangesTheHash()
    {
        var before = ShaderSourceHash.Compute([Dependency("a.slang", "x")]);
        var after = ShaderSourceHash.Compute([Dependency("b.slang", "x")]);

        Assert.That(after, Is.Not.EqualTo(before));
    }

    [Test]
    public void AbsoluteLocationDoesNotAffectTheHash()
    {
        var first = Dependency("shaders/a.slang", "x");
        var second = Dependency("shaders/a.slang", "x");

        Assert.That(first.AbsolutePath, Is.Not.EqualTo(second.AbsolutePath));
        Assert.That(ShaderSourceHash.Compute([first]), Is.EqualTo(ShaderSourceHash.Compute([second])));
    }

    [Test]
    public void DependencyOrderDoesNotAffectTheHash()
    {
        var a = Dependency("a.slang", "a");
        var b = Dependency("b.slang", "b");

        Assert.That(ShaderSourceHash.Compute([a, b]), Is.EqualTo(ShaderSourceHash.Compute([b, a])));
    }

    [Test]
    public void AddingADependencyChangesTheHash()
    {
        var a = Dependency("a.slang", "a");

        Assert.That(ShaderSourceHash.Compute([a, Dependency("b.slang", "b")]),
            Is.Not.EqualTo(ShaderSourceHash.Compute([a])));
    }
}
