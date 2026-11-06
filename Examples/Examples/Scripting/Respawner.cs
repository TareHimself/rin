using Rin.World;
using Rin.World.Scripting;
using Rin.World.Tasks;

namespace Examples.Scripting;

/// <summary>
///     Waiting on another entity: <see cref="TaskContext.Until" /> polls whether the spawned entity is still alive, and a
///     delay later a new one replaces it.
/// </summary>
public sealed class Respawner(string label, float delaySeconds, Func<Entity> spawn) : Script
{
    protected override void Start() => Tasks.Run(KeepOneAlive);

    private async GameTask KeepOneAlive(TaskContext ctx)
    {
        while (true)
        {
            var current = spawn();
            await ctx.Until(() => !World.IsAlive(current));
            ScriptLog.Write(this, $"the {label} is gone, a new one in {delaySeconds} s");
            await ctx.Seconds(delaySeconds);
        }
    }
}
