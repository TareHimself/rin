using Examples.Editor.Model;
using Rin.World;

namespace Examples.Editor;

/// <summary>
///     Runs the world for the editor: frozen while editing, played from a snapshot of the edited world, and put back to
///     that snapshot on Stop.
/// </summary>
/// <remarks>
///     A step runs on the next <see cref="Update" />, which adds exactly one tick interval to a paused world so it runs one
///     tick whatever its tick accumulator held.
/// </remarks>
public sealed class PlaySession(World world)
{
    private readonly WorldSnapshot _editState = new();
    private bool _stepRequested;

    public PlayState State { get; private set; } = PlayState.Editing;

    public event Action? Changed;

    public bool CanApply(PlayCommand command) => PlayTransitions.TryNext(State, command, out _);

    public void Apply(PlayCommand command)
    {
        if (!PlayTransitions.TryNext(State, command, out var next))
            throw new InvalidOperationException($"{command} is not allowed while {State}.");

        RunEffect(command);
        State = next;
        Changed?.Invoke();
    }

    public void Update(float deltaSeconds)
    {
        if (!_stepRequested)
        {
            world.Update(deltaSeconds);
            return;
        }

        _stepRequested = false;
        world.TimeScale = 1f;
        world.Update(world.TickInterval);
        world.TimeScale = 0f;
    }

    public void Clear() => _editState.Clear();

    private void RunEffect(PlayCommand command)
    {
        switch (command)
        {
            case PlayCommand.Play:
                // Restoring straight away rebuilds the physics bodies, so bodies moved while editing start there.
                world.SaveSnapshot(_editState);
                world.RestoreSnapshot(_editState);
                world.TimeScale = 1f;
                break;
            case PlayCommand.Pause:
                world.TimeScale = 0f;
                break;
            case PlayCommand.Resume:
                world.TimeScale = 1f;
                break;
            case PlayCommand.Step:
                _stepRequested = true;
                break;
            case PlayCommand.Stop:
                world.RestoreSnapshot(_editState);
                world.TimeScale = 0f;
                break;
        }
    }
}
