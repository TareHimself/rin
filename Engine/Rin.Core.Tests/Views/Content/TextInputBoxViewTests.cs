using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Events;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;

namespace Rin.Core.Tests.Views.Content;

public class TextInputBoxViewTests
{
    [SetUp]
    public void SetUp()
    {
        IApplication.Override = new FakeApplication();
        IViewsModule.Override = new FakeViewsModule();
    }

    [TearDown]
    public void TearDown()
    {
        IApplication.Override = null;
        IViewsModule.Override = null;
    }

    [Test]
    public void TypingAppendsAtTheCaret()
    {
        var input = new TextInputBoxView();

        Type(input, "abc");

        Assert.That(input.Content, Is.EqualTo("abc"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void BackspaceRemovesTheCharacterBeforeTheCaretEachTime()
    {
        var input = new TextInputBoxView();
        Type(input, "abcd");

        Press(input, InputKey.Backspace);
        Press(input, InputKey.Backspace);

        Assert.That(input.Content, Is.EqualTo("ab"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
    }

    [Test]
    public void BackspaceDownToEmptyThenTypingWorks()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");

        for (var i = 0; i < 5; i++) Press(input, InputKey.Backspace);
        Type(input, "x");

        Assert.That(input.Content, Is.EqualTo("x"));
        Assert.That(input.CursorPosition, Is.EqualTo(0));
    }

    [Test]
    public void TypingInTheMiddleInsertsAfterTheCaret()
    {
        var input = new TextInputBoxView();
        Type(input, "ac");
        Press(input, InputKey.Left);

        Type(input, "b");

        Assert.That(input.Content, Is.EqualTo("abc"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
    }

    [Test]
    public void DeleteRemovesTheCharacterAfterTheCaret()
    {
        var input = new TextInputBoxView();
        Type(input, "abc");
        Press(input, InputKey.Home);

        Press(input, InputKey.Delete);

        Assert.That(input.Content, Is.EqualTo("bc"));
        Assert.That(input.CursorPosition, Is.EqualTo(-1));
    }

    [Test]
    public void HomeAndEndMoveTheCaret()
    {
        var input = new TextInputBoxView();
        Type(input, "abc");

        Press(input, InputKey.Home);
        Assert.That(input.CursorPosition, Is.EqualTo(-1));

        Press(input, InputKey.End);
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void AssigningContentFromOutsidePutsTheCaretAtTheEnd()
    {
        var input = new TextInputBoxView { Content = "guest" };

        Type(input, "!");

        Assert.That(input.Content, Is.EqualTo("guest!"));
    }

    [Test]
    public void ClearingContentThenBackspaceDoesNotThrow()
    {
        var input = new TextInputBoxView();
        Type(input, "abc");

        input.Content = string.Empty;

        Assert.DoesNotThrow(() => Press(input, InputKey.Backspace));
        Assert.That(input.CursorPosition, Is.EqualTo(-1));
    }

    [Test]
    public void PreeditShowsAtTheCaretAndEachOneReplacesTheLast()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");
        Press(input, InputKey.Left);

        Preedit(input, "k", 1);
        Preedit(input, "ka", 2);

        Assert.That(input.Content, Is.EqualTo("akab"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void CommitReplacesThePreedit()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");
        Preedit(input, "ka", 2);

        Type(input, "か");

        Assert.That(input.Content, Is.EqualTo("abか"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void EmptyPreeditRemovesTheComposition()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");
        Preedit(input, "ka", 2);

        Preedit(input, string.Empty, 0);

        Assert.That(input.Content, Is.EqualTo("ab"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
    }

    [Test]
    public void LosingFocusDropsThePreedit()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");
        Preedit(input, "ka", 2);

        input.OnFocusLost();

        Assert.That(input.Content, Is.EqualTo("ab"));
    }

    [Test]
    public void EscapeClearsFocus()
    {
        var input = new TextInputBoxView();

        Assert.That(PressEscape(input), Is.EqualTo(1));
    }

    [Test]
    public void EscapeKeepsFocusWhenTurnedOff()
    {
        var input = new TextInputBoxView { UnfocusOnEscape = false };

        Assert.That(PressEscape(input), Is.Zero);
    }

    [Test]
    public void EscapeKeepsFocusWhileComposing()
    {
        var input = new TextInputBoxView();
        Preedit(input, "ka", 2);

        Assert.That(PressEscape(input), Is.Zero);
        Assert.That(input.Content, Is.EqualTo("ka"));
    }

    [Test]
    public void ClickingWhileComposingDropsThePreedit()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");
        Preedit(input, "ka", 2);

        input.OnCursorDown(new CursorDownSurfaceEvent(new FakeSurface(), CursorButton.One, Vector2.Zero),
            Matrix4x4.Identity);

        Assert.That(input.Content, Is.EqualTo("ab"));
    }

    private static int PressEscape(TextInputBoxView input)
    {
        var surface = new FakeSurface();
        input.OnKeyboard(new KeyboardSurfaceEvent(surface, InputKey.Escape, InputState.Pressed));
        return surface.FocusClears;
    }

    private static void Preedit(TextInputBoxView input, string text, int caret)
    {
        input.OnTextPreedit(new TextPreeditSurfaceEvent(new FakeSurface(), text, caret, caret));
    }

    private static void Type(TextInputBoxView input, string text)
    {
        foreach (var character in text) input.OnCharacter(new CharacterSurfaceEvent(new FakeSurface(), character, 0));
    }

    private static void Press(TextInputBoxView input, InputKey key)
    {
        input.OnKeyboard(new KeyboardSurfaceEvent(new FakeSurface(), key, InputState.Pressed));
    }

    private sealed class FakeViewsModule : IViewsModule
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

    private sealed class FakeFontManager : IFontManager
    {
        public Task Prepare(IFont font, IEnumerable<char> characters) => Task.CompletedTask;
        public void LoadFont(Stream fileStream) { }
        public LiveGlyphInfo GetGlyph(IFont font, char character) => throw new NotSupportedException();
        public IFont? GetFont(string name) => null;

        public GlyphRect[] MeasureText(IFont font, in ReadOnlySpan<char> text, float size,
            float maxWidth = float.PositiveInfinity) => [];

        public float GetPixelRange() => 0f;
        public IEnumerable<IFont> GetFonts() => [];
        public void Dispose() { }
    }
}
