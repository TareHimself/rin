using Examples.Editor.Model;

namespace Examples.Tests.Editor.Model;

public class PlayTransitionsTests
{
    private static readonly (PlayState From, PlayCommand Command, PlayState To)[] Allowed =
    [
        (PlayState.Editing, PlayCommand.Play, PlayState.Playing),
        (PlayState.Playing, PlayCommand.Pause, PlayState.Paused),
        (PlayState.Playing, PlayCommand.Stop, PlayState.Editing),
        (PlayState.Paused, PlayCommand.Resume, PlayState.Playing),
        (PlayState.Paused, PlayCommand.Step, PlayState.Paused),
        (PlayState.Paused, PlayCommand.Stop, PlayState.Editing)
    ];

    [Test]
    public void EveryListedMoveLeadsToItsStateAndEveryOtherMoveIsRefused()
    {
        foreach (var state in Enum.GetValues<PlayState>())
        foreach (var command in Enum.GetValues<PlayCommand>())
        {
            var listed = Allowed.Where(move => move.From == state && move.Command == command).ToArray();

            var accepted = PlayTransitions.TryNext(state, command, out var next);

            Assert.That(accepted, Is.EqualTo(listed.Length == 1), $"{command} while {state}");
            if (accepted) Assert.That(next, Is.EqualTo(listed[0].To), $"{command} while {state}");
        }
    }
}
