using Rin.World.Ecs.Scheduling;

namespace Rin.World.Ecs.Scripting;

internal sealed class ScriptTick(ScriptFeature feature) : TickSystem
{
    public override void Tick(in TickContext context) => feature.TickScripts(context);
}
