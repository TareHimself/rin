using Examples.Common;
using Rin.World.Scripting;

namespace Examples.Scripting;

/// <summary>
///     Writes a script's events to the console with the tick they happened on, so the timing of each await is visible.
/// </summary>
public static class ScriptLog
{
    private static readonly Log Log = Log.For("scripting");

    public static void Write(Script script, string message)
    {
        Log.Info($"[tick {script.World.TickIndex,5}] {script.GetType().Name,-13} {message}");
    }
}
