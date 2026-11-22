namespace Examples.Editor.Model;

public enum PlayState
{
    Editing,
    Playing,
    Paused
}

public enum PlayCommand
{
    Play,
    Pause,
    Resume,
    Step,
    Stop
}

/// <summary>
///     Which <see cref="PlayCommand" /> each <see cref="PlayState" /> accepts, and the state it leads to.
/// </summary>
public static class PlayTransitions
{
    private static readonly Dictionary<(PlayState, PlayCommand), PlayState> Moves = new()
    {
        [(PlayState.Editing, PlayCommand.Play)] = PlayState.Playing,
        [(PlayState.Playing, PlayCommand.Pause)] = PlayState.Paused,
        [(PlayState.Playing, PlayCommand.Stop)] = PlayState.Editing,
        [(PlayState.Paused, PlayCommand.Resume)] = PlayState.Playing,
        [(PlayState.Paused, PlayCommand.Step)] = PlayState.Paused,
        [(PlayState.Paused, PlayCommand.Stop)] = PlayState.Editing
    };

    public static bool TryNext(PlayState state, PlayCommand command, out PlayState next) =>
        Moves.TryGetValue((state, command), out next);
}
