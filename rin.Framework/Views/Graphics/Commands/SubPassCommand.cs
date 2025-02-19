using rin.Framework.Core;

namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
///     Used to run custom passes, i.e. render a scene before displaying it in a viewport, all commands of this nature will
///     run
/// </summary>
public abstract class SubPassCommand : Disposable
{
    protected override void OnDispose(bool isManual)
    {
    }
}