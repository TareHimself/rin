using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Tests.Views.Graphics.Commands;

public class CommandExtensionsTests
{
    [Test]
    public void CastToArrayCopiesTheCommandsInOrder()
    {
        var first = new NoOpCommand();
        var second = new NoOpCommand();
        ICommand[] source = [first, second];

        var copy = new ReadOnlySpan<ICommand>(source).CastToArray<NoOpCommand>();
        Array.Clear(source);

        Assert.That(copy, Is.EqualTo(new[] { first, second }).Using<NoOpCommand>(ReferenceEquals));
    }

    [Test]
    public void CastToArrayThrowsForACommandOfAnotherType()
    {
        ICommand[] commands = [new NoOpCommand()];

        Assert.Throws<InvalidCastException>(() => new ReadOnlySpan<ICommand>(commands).CastToArray<OtherCommand>());
    }

    private sealed class OtherCommand : NoOpCommand;
}
