using System.Numerics;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Shared;
using Rin.Core.Views;
using Rin.Core.Views.Events;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;

namespace Rin.Core.Tests.Views;

/// <summary>Minimal <see cref="IApplication" /> so <c>View</c> construction (AnimationRunner) works headlessly.</summary>
internal sealed class FakeApplication : IApplication
{
    public float TimeSeconds => 0f;
    public float LastDeltaSeconds => 0f;
    public Dispatcher MainDispatcher { get; } = new();
    public Dispatcher RenderDispatcher { get; } = new();

    public event Action? OnPreUpdate;
    public event Action<float>? OnUpdate;
    public event Action? OnPostUpdate;
    public event Action? OnCollect;
    public event Action? OnPreRender;
    public event Action? OnRender;
    public event Action? OnPostRender;

    public IGraphicsModule CreateGraphicsModule() => throw new NotSupportedException();
    public IAudioModule CreateAudioModule() => throw new NotSupportedException();
    public IViewsModule CreateViewsModule() => throw new NotSupportedException();
    public void Run() => throw new NotSupportedException();
    public void RequestExit() { }
    public string[] SelectFile(string title = "Select File's", bool multiple = false, string filter = "") => [];
    public string[] SelectPath(string title = "Select Path's", bool multiple = false) => [];

    public void Dispose()
    {
        _ = OnPreUpdate;
        _ = OnUpdate;
        _ = OnPostUpdate;
        _ = OnCollect;
        _ = OnPreRender;
        _ = OnRender;
        _ = OnPostRender;
    }
}

/// <summary>Inert <see cref="ISurface" /> — just an identity to hand views so <c>Surface</c> gets set.</summary>
internal sealed class FakeSurface : ISurface
{
    public IView? FocusedView => null;
    public event Action<CursorUpSurfaceEvent>? OnCursorUp;

    public Vector2 GetCursorPosition() => Vector2.Zero;
    public void SetCursorPosition(Vector2 position) { }
    public void StartTyping(View view) { }
    public void StopTyping(View view) { }
    public void SetTypingRect(View view, in Rect2D caret) { }
    public IClipboard Clipboard { get; } = new InMemoryClipboard();
    public void Init() { }
    public Vector2 GetSize() => new(1000f);
    public int FocusClears { get; private set; }
    public void ClearFocus() => FocusClears++;
    public bool RequestFocus(IView requester) => false;
    public CommandList? CollectCommands() => null;
    public void ReceiveCursorEnter(CursorMoveSurfaceEvent e) { }
    public void ReceiveCursorLeave() { }
    public void ReceiveResize(ResizeSurfaceEvent e) { }
    public void ReceiveCursorDown(CursorDownSurfaceEvent e) { }
    public void ReceiveCursorUp(CursorUpSurfaceEvent e) => OnCursorUp?.Invoke(e);
    public void ReceiveCursorMove(CursorMoveSurfaceEvent e) { }
    public void ReceiveScroll(ScrollSurfaceEvent e) { }
    public void ReceiveCharacter(CharacterSurfaceEvent e) { }
    public void ReceiveTextPreedit(TextPreeditSurfaceEvent e) { }
    public void ReceiveKeyboard(KeyboardSurfaceEvent e) { }
    public T Add<T>() where T : IView, new() => throw new NotSupportedException();
    public T Add<T>(T view) where T : IView => throw new NotSupportedException();
    public bool Remove(IView view) => false;
    public void OnViewLayoutInvalidated(IView view) { }
    public void ForceLayout() { }
    public void Update(float deltaTime) { }
    public void Dispose() { }
}

internal sealed class FakeViewsModule : IViewsModule
{
    public IFontManager FontManager { get; } = new FakeFontManager();
    public string DefaultFontFamily { get; set; } = "Noto Sans";
    public event Action<IWindowSurface>? OnSurfaceCreated;
    public event Action<IWindowSurface>? OnSurfaceDestroyed;

    public void Start(IApplication app) { }
    public void Stop(IApplication app) { }
    public void Update(float deltaTime) { }
    public void AddFont(string fontPath) { }
    public IBatcher GetBatcher<T>() where T : IBatcher, new() => throw new NotSupportedException();
    public IWindowSurface? GetWindowSurface(IWindowRenderer renderer) => null;
    public IWindowSurface? GetWindowSurface(IWindow window) => null;
}

// A monospace font, so cursor positions map to characters without a real font.
internal sealed class FakeFontManager : IFontManager
{
    private readonly FakeFont _font;

    public FakeFontManager()
    {
        _font = new FakeFont(this);
    }

    public Task Prepare(IFont font, IEnumerable<char> characters) => Task.CompletedTask;
    public void LoadFont(Stream fileStream) { }

    public LiveGlyphState GlyphState { get; set; } = LiveGlyphState.Invalid;

    public LiveGlyphInfo GetGlyph(IFont font, char character) => new()
    {
        AtlasHandle = default,
        Coordinate = default,
        Size = default,
        State = GlyphState
    };

    public IFont? GetFont(string name) => _font;

    public int MeasureText(IFont font, in ReadOnlySpan<char> text, float size, Span<GlyphRect> into,
        float maxWidth = float.PositiveInfinity) => FakeFont.Measure(text, into);

    public float GetPixelRange() => 0f;
    public IEnumerable<IFont> GetFonts() => [_font];
    public void Dispose() { }
}

internal sealed class FakeFont(IFontManager manager) : IFont
{
    public const float Advance = 10f;
    public const float LineHeight = 20f;

    public string Name => "fake";
    public IFontManager FontManager => manager;
    public float GetLineHeight(float fontSize) => LineHeight;

    public int MeasureText(in ReadOnlySpan<char> text, float size, Span<GlyphRect> into,
        float maxWidth = float.PositiveInfinity)
    {
        return Measure(text, into);
    }

    public IEnumerable<CodePoint> GetCodePoints() => [];

    public static int Measure(ReadOnlySpan<char> text, Span<GlyphRect> bounds)
    {
        var column = 0;
        var line = 0;
        for (var i = 0; i < text.Length; i++)
        {
            bounds[i] = new GlyphRect
            {
                Character = text[i],
                PenX = column * Advance,
                Advance = Advance,
                LineTop = line * LineHeight,
                LineHeight = LineHeight
            };
            column++;
            if (text[i] != '\n') continue;
            column = 0;
            line++;
        }

        return text.Length;
    }
}
