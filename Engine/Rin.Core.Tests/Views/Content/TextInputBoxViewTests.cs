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
