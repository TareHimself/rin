namespace Rin.World.Tasks;

/// <summary>
///     Why a <see cref="GameTask" /> was cancelled.
/// </summary>
public enum CancelReason : byte
{
    /// <summary>
    ///     <see cref="GameTaskScheduler.Cancel" /> or <see cref="GameTaskScheduler.CancelAll" />, a <c>WhenAny</c> loser, or
    ///     the other task of a <c>WhenAll</c> that faulted.
    /// </summary>
    Cancelled,

    /// <summary>
    ///     The owner entity was despawned.
    /// </summary>
    Despawned,

    /// <summary>
    ///     The <see cref="Scripting.Script" /> that started the task was detached.
    /// </summary>
    Detached,

    /// <summary>
    ///     A snapshot restore, or <see cref="Scripting.ScriptFeature.Restore" />.
    /// </summary>
    Restored
}
