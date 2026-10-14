using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests;

public class SlangDiagnosticTests
{
    [Test]
    public void Parses_diagnostic_lines_and_skips_source_excerpts()
    {
        const string output = """
            Failed to load slang shader module:
            Shaders/Rin/Core/Views/blur.slang(45): error 30027: 'tnt' is not a member of 'BlurData'.
                    result *= data.tnt;
                                   ^~~
            Shaders/Rin/Core/Views/blur.slang(12,7): warning 39999: something minor
            """;

        var diagnostics = SlangDiagnostic.Parse(output);

        Assert.That(diagnostics, Has.Count.EqualTo(2));
        Assert.That(diagnostics[0], Is.EqualTo(new SlangDiagnostic("Shaders/Rin/Core/Views/blur.slang", 45, 0,
            "error", "30027", "'tnt' is not a member of 'BlurData'.")));
        Assert.That(diagnostics[1].Line, Is.EqualTo(12));
        Assert.That(diagnostics[1].Column, Is.EqualTo(7));
        Assert.That(diagnostics[1].Severity, Is.EqualTo("warning"));
    }

    [Test]
    public void Returns_nothing_for_output_without_diagnostics()
    {
        Assert.That(SlangDiagnostic.Parse("Failed to create composed program."), Is.Empty);
    }
}
