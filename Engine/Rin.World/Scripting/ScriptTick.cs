using Rin.World.Scheduling;

namespace Rin.World.Scripting;

internal sealed class ScriptTick(ScriptFeature feature) : TickSystem
{
    public override void Tick(in TickContext context) => feature.TickScripts(context);
}
