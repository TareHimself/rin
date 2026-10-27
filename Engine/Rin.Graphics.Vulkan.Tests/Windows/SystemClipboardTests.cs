using Rin.Graphics.Vulkan.Windows;

namespace Rin.Graphics.Vulkan.Tests.Windows;

public class SystemClipboardTests
{
    private readonly SystemClipboard _clipboard = new();

    [SetUp]
    public void ResetFake()
    {
        FakeNative.Reset();
    }

    [Test]
    public void GetText_WithNothingOnTheClipboard_ReturnsNull()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_clipboard.HasText, Is.False);
            Assert.That(_clipboard.GetText(), Is.Null);
        });
    }

    [TestCase("hello")]
    [TestCase("héllo 😀 world")]
    [TestCase("")]
    public void SetText_ThenGetText_RoundTrips(string text)
    {
        var accepted = _clipboard.SetText(text);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(_clipboard.HasText, Is.True);
            Assert.That(_clipboard.GetText(), Is.EqualTo(text));
        });
    }

    [Test]
    public void Clear_LeavesNoText()
    {
        _clipboard.SetText("hello");

        _clipboard.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(_clipboard.HasText, Is.False);
            Assert.That(_clipboard.GetText(), Is.Null);
        });
    }
}
