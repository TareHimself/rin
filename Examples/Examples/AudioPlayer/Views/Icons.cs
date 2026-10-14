using Rin.Core.Views;

namespace Examples.AudioPlayer.Views;

/// <summary>
///     Font Awesome 6 Free solid codepoints, loaded from the font in <c>assets/fonts</c>.
/// </summary>
public static class Icons
{
    public const char Play = '';
    public const char Pause = '';
    public const char Previous = '';
    public const char Next = '';
    public const char Repeat = '';
    public const char VolumeHigh = '';
    public const char VolumeLow = '';
    public const char VolumeMuted = '';
    public const char Plus = '';
    public const char Music = '';
    public const char Remove = '';

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
