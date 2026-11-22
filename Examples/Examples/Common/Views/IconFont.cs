using Rin.Core.Views;

namespace Examples.Common.Views;

/// <summary>
///     The Font Awesome 6 Free solid font in <c>assets/fonts</c>, whose icons sit at private-use codepoints.
/// </summary>
public static class IconFont
{
    private const string FontPath = "fonts/fa-solid-900.ttf";
    private const string FamilyKeyword = "Awesome";

    public static string Family { get; private set; } = string.Empty;

    public static void Load()
    {
        var fonts = IViewsModule.Get().FontManager;
        if (FindFamily() is null)
        {
            using var stream = File.OpenRead(ExampleHost.AssetPath(FontPath));
            fonts.LoadFont(stream);
        }

        Family = FindFamily() ?? throw new InvalidOperationException($"{FontPath} did not load");
    }

    private static string? FindFamily()
    {
        return IViewsModule.Get().FontManager.GetFonts()
            .FirstOrDefault(f => f.Name.Contains(FamilyKeyword, StringComparison.OrdinalIgnoreCase))?.Name;
    }
}
