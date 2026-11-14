using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Tests.Views.Graphics.Commands;

public class CommandExtensionsTests
{
    [Test]
    public void CastToArrayCopiesTheCommandsInOrder()
    {
        List<ICommand> source = [new NoOpCommand(), new NoOpCommand()];
        ReadOnlySpan<ICommand> commands = source.ToArray();

        var copy = commands.CastToArray<NoOpCommand>();
        source.Clear();

        Assert.That(copy, Has.Length.EqualTo(2));
        Assert.That(copy[0], Is.Not.SameAs(copy[1]));
    }

    [Test]
    public void CastToArrayThrowsForACommandOfAnotherType()
    {
        ICommand[] commands = [new NoOpCommand()];

        Assert.Throws<InvalidCastException>(() => new ReadOnlySpan<ICommand>(commands).CastToArray<OtherCommand>());
    }

    private sealed class OtherCommand : NoOpCommand;
}
