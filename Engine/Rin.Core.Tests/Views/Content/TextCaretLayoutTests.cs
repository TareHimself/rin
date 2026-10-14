using System.Numerics;
using Rin.Core.Views.Content;
using Rin.Core.Views.Font;

namespace Rin.Core.Tests.Views.Content;

public class TextCaretLayoutTests
{
    private const float Advance = 10f;
    private const float LineHeight = 20f;

    [Test]
    public void EmptyTextPutsTheCaretAtTheStart()
    {
        Assert.That(TextCaretLayout.IndexAt([], new Vector2(50f, 5f)), Is.EqualTo(-1));
    }

    [TestCase(-5f, -1)]
    [TestCase(4f, -1)]
    [TestCase(6f, 0)]
    [TestCase(14f, 0)]
    [TestCase(16f, 1)]
    [TestCase(500f, 2)]
    public void PointSnapsToTheNearestCharacterEdge(float x, int expected)
    {
        var bounds = Measure("abc");

        Assert.That(TextCaretLayout.IndexAt(bounds, new Vector2(x, 5f)), Is.EqualTo(expected));
    }

    [Test]
    public void PointPastTheEndOfALineStaysBeforeItsLineBreak()
    {
        var bounds = Measure("ab\ncd");

        Assert.That(TextCaretLayout.IndexAt(bounds, new Vector2(500f, 5f)), Is.EqualTo(1));
    }

    [TestCase(-5f, 2)]
    [TestCase(11f, 3)]
    [TestCase(500f, 4)]
    public void PointOnTheSecondLineIndexesIntoThatLine(float x, int expected)
    {
        var bounds = Measure("ab\ncd");

        Assert.That(TextCaretLayout.IndexAt(bounds, new Vector2(x, LineHeight + 5f)), Is.EqualTo(expected));
    }

    [Test]
    public void PointBelowTheTextUsesTheLastLine()
    {
        var bounds = Measure("ab\ncd");

        Assert.That(TextCaretLayout.IndexAt(bounds, new Vector2(500f, 900f)), Is.EqualTo(4));
    }

    [TestCase(0f, 150f, 50f)]
    [TestCase(80f, 30f, 30f)]
    [TestCase(20f, 60f, 20f)]
    public void ScrollMovesOnlyAsFarAsNeededToShowTheCaret(float scroll, float caretX, float expected)
    {
        Assert.That(TextCaretLayout.ScrollToShow(scroll, caretX, caretX, 100f, 300f), Is.EqualTo(expected));
    }

    [Test]
    public void ScrollDoesNotShowSpacePastTheText()
    {
        Assert.That(TextCaretLayout.ScrollToShow(200f, 120f, 120f, 100f, 120f), Is.EqualTo(20f));
    }

    [Test]
    public void TextThatFitsDoesNotScroll()
    {
        Assert.That(TextCaretLayout.ScrollToShow(40f, 60f, 60f, 100f, 60f), Is.Zero);
    }

    [Test]
    public void ScrollShowsTheEndOfASpanBelowTheView()
    {
        Assert.That(TextCaretLayout.ScrollToShow(0f, 100f, 120f, 100f, 300f), Is.EqualTo(20f));
    }

    [Test]
    public void ClampKeepsTheScrollInsideTheText()
    {
        Assert.That(TextCaretLayout.ClampScroll(-10f, 100f, 300f), Is.Zero);
        Assert.That(TextCaretLayout.ClampScroll(500f, 100f, 300f), Is.EqualTo(200f));
    }

    [Test]
    public void SizeIsTheWidestLineAndEveryLine()
    {
        Assert.That(TextCaretLayout.Size(Measure("abc\nd")), Is.EqualTo(new Vector2(40f, 2f * LineHeight)));
    }

    [Test]
    public void SizeCountsTheEmptyLineAfterATrailingLineBreak()
    {
        Assert.That(TextCaretLayout.Size(Measure("ab\n")).Y, Is.EqualTo(2f * LineHeight));
    }

    private static GlyphRect[] Measure(string text)
    {
        var bounds = new GlyphRect[text.Length];
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

        return bounds;
    }
}
