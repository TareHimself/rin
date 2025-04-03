namespace Rin.Engine.Views.Graphics.Commands;

/// <summary>
///     Base class for commands
/// </summary>
public abstract class Command : Disposable
{
    protected override void OnDispose(bool isManual)
    {
    }
}