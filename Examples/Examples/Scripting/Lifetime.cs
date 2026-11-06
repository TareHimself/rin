using Rin.World.Scripting;
using Rin.World.Tasks;

namespace Examples.Scripting;

/// <summary>
///     The smallest task: wait, then act. Despawns its entity after a few seconds.
/// </summary>
public sealed class Lifetime(float seconds) : Script
{
    protected override void Start() => Tasks.Run(DespawnLater);

    private async GameTask DespawnLater(TaskContext ctx)
    {
        await ctx.Seconds(seconds);
        World.Despawn(Entity);
    }
}
