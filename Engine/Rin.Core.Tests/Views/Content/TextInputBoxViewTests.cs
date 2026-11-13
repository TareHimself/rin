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

    [Test]
    public void PasteInsertsTheClipboardAtTheCaret()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        surface.Clipboard.SetText("XY");
        Type(input, "abcd");
        Press(input, InputKey.Left);
        Press(input, InputKey.Left);

        Shortcut(input, surface, InputKey.V);

        Assert.That(input.Content, Is.EqualTo("abXYcd"));
        Assert.That(input.CursorPosition, Is.EqualTo(3));
    }

    [Test]
    public void PasteWithAnEmptyClipboardChangesNothing()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");

        Shortcut(input, new FakeSurface(), InputKey.V);

        Assert.That(input.Content, Is.EqualTo("ab"));
    }

    [Test]
    public void CopyAndCutWithoutASelectionChangeNothing()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        Type(input, "hello");

        Shortcut(input, surface, InputKey.C);
        Shortcut(input, surface, InputKey.X);

        Assert.That(surface.Clipboard.HasText, Is.False);
        Assert.That(input.Content, Is.EqualTo("hello"));
    }

    [Test]
    public void ShiftArrowsExtendTheSelectionFromTheCaret()
    {
        var input = new TextInputBoxView();
        Type(input, "abcd");

        Press(input, InputKey.Left, InputModifier.Shift);
        Press(input, InputKey.Left, InputModifier.Shift);

        Assert.That(input.HasSelection, Is.True);
        Assert.That(input.SelectedText, Is.EqualTo("cd"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
        Assert.That((input.SelectionStart, input.SelectionEnd), Is.EqualTo((1, 3)));
    }

    [Test]
    public void AnArrowWithoutShiftCollapsesTheSelectionToItsNearEnd()
    {
        var input = new TextInputBoxView();
        Type(input, "abcd");
        Press(input, InputKey.Home);
        Press(input, InputKey.Right, InputModifier.Shift);
        Press(input, InputKey.Right, InputModifier.Shift);

        Press(input, InputKey.Left);

        Assert.That(input.HasSelection, Is.False);
        Assert.That(input.CursorPosition, Is.EqualTo(-1));

        Press(input, InputKey.Right, InputModifier.Shift);
        Press(input, InputKey.Right);

        Assert.That(input.HasSelection, Is.False);
        Assert.That(input.CursorPosition, Is.EqualTo(0));
    }

    [Test]
    public void ShiftHomeSelectsBackToTheStart()
    {
        var input = new TextInputBoxView();
        Type(input, "abc");

        Press(input, InputKey.Home, InputModifier.Shift);

        Assert.That(input.SelectedText, Is.EqualTo("abc"));
        Assert.That(input.CursorPosition, Is.EqualTo(-1));
    }

    [Test]
    public void ControlASelectsEverything()
    {
        var input = new TextInputBoxView();
        Type(input, "abc");
        Press(input, InputKey.Left);

        Shortcut(input, new FakeSurface(), InputKey.A);

        Assert.That(input.SelectedText, Is.EqualTo("abc"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void TypingReplacesTheSelection()
    {
        var input = new TextInputBoxView();
        Type(input, "abcd");
        Press(input, InputKey.Left);
        Press(input, InputKey.Left, InputModifier.Shift);
        Press(input, InputKey.Left, InputModifier.Shift);

        Type(input, "X");

        Assert.That(input.Content, Is.EqualTo("aXd"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
        Assert.That(input.HasSelection, Is.False);
    }

    [Test]
    public void BackspaceAndDeleteRemoveTheSelection()
    {
        var input = new TextInputBoxView();
        Type(input, "abcd");
        Press(input, InputKey.Left, InputModifier.Shift);
        Press(input, InputKey.Backspace);

        Assert.That(input.Content, Is.EqualTo("abc"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));

        Press(input, InputKey.Home);
        Press(input, InputKey.Right, InputModifier.Shift);
        Press(input, InputKey.Delete);

        Assert.That(input.Content, Is.EqualTo("bc"));
        Assert.That(input.CursorPosition, Is.EqualTo(-1));
    }

    [Test]
    public void CopyPutsTheSelectionOnTheClipboard()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        Type(input, "hello");
        Press(input, InputKey.Left, InputModifier.Shift);
        Press(input, InputKey.Left, InputModifier.Shift);

        Shortcut(input, surface, InputKey.C);

        Assert.That(surface.Clipboard.GetText(), Is.EqualTo("lo"));
        Assert.That(input.Content, Is.EqualTo("hello"));
        Assert.That(input.HasSelection, Is.True);
    }

    [Test]
    public void CutMovesTheSelectionToTheClipboard()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        Type(input, "hello");
        Press(input, InputKey.Left, InputModifier.Shift);
        Press(input, InputKey.Left, InputModifier.Shift);

        Shortcut(input, surface, InputKey.X);

        Assert.That(surface.Clipboard.GetText(), Is.EqualTo("lo"));
        Assert.That(input.Content, Is.EqualTo("hel"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void PasteReplacesTheSelection()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        surface.Clipboard.SetText("XY");
        Type(input, "abcd");
        Press(input, InputKey.Home);
        Press(input, InputKey.Right, InputModifier.Shift);
        Press(input, InputKey.Right, InputModifier.Shift);

        Shortcut(input, surface, InputKey.V);

        Assert.That(input.Content, Is.EqualTo("XYcd"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
    }

    [Test]
    public void APreeditReplacesTheSelection()
    {
        var input = new TextInputBoxView();
        Type(input, "abcd");
        Press(input, InputKey.Left, InputModifier.Shift);

        Preedit(input, "k", 1);

        Assert.That(input.Content, Is.EqualTo("abck"));
        Assert.That(input.HasSelection, Is.False);
    }

    [Test]
    public void AssigningContentFromOutsideClearsTheSelection()
    {
        var input = new TextInputBoxView();
        Type(input, "abc");
        Shortcut(input, new FakeSurface(), InputKey.A);

        input.Content = "xyz";

        Assert.That(input.HasSelection, Is.False);
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void DraggingAcrossTheTextSelectsIt()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        input.Content = "abcd";

        input.OnCursorDown(new CursorDownSurfaceEvent(surface, CursorButton.One, new Vector2(FakeFont.Advance * 1.2f, 5f)),
            Matrix4x4.Identity);
        input.OnCursorMove(new CursorMoveSurfaceEvent(surface, new Vector2(FakeFont.Advance * 3.2f, 5f)),
            Matrix4x4.Identity);
        input.OnCursorUp(new CursorUpSurfaceEvent(surface, CursorButton.One, Vector2.Zero));

        Assert.That(input.SelectedText, Is.EqualTo("bc"));
        Assert.That(input.CursorPosition, Is.EqualTo(2));
    }

    [Test]
    public void AClickWithoutADragLeavesNoSelection()
    {
        var input = new TextInputBoxView();
        var surface = new FakeSurface();
        input.Content = "abcd";

        input.OnCursorDown(new CursorDownSurfaceEvent(surface, CursorButton.One, new Vector2(FakeFont.Advance * 1.2f, 5f)),
            Matrix4x4.Identity);
        input.OnCursorUp(new CursorUpSurfaceEvent(surface, CursorButton.One, Vector2.Zero));
        input.OnCursorMove(new CursorMoveSurfaceEvent(surface, new Vector2(FakeFont.Advance * 3.2f, 5f)),
            Matrix4x4.Identity);

        Assert.That(input.HasSelection, Is.False);
        Assert.That(input.CursorPosition, Is.EqualTo(0));
    }

    [Test]
    public void AControlKeyIsNotTypedAsAShortcutOrACharacter()
    {
        var input = new TextInputBoxView();
        Type(input, "ab");

        Shortcut(input, new FakeSurface(), InputKey.Left);

        Assert.That(input.Content, Is.EqualTo("ab"));
        Assert.That(input.CursorPosition, Is.EqualTo(1));
    }

    private static void Shortcut(TextInputBoxView input, FakeSurface surface, InputKey key)
    {
        input.OnKeyboard(new KeyboardSurfaceEvent(surface, key, InputState.Pressed, InputModifier.Control));
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

    private static void Press(TextInputBoxView input, InputKey key, InputModifier modifiers = InputModifier.None)
    {
        input.OnKeyboard(new KeyboardSurfaceEvent(new FakeSurface(), key, InputState.Pressed, modifiers));
    }
}
