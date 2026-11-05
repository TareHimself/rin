using Rin.World.Scripting;
using Rin.World.Tasks;

namespace Examples.SceneTest;

public sealed class BoxRain(float intervalSeconds, Action dropBox) : Script
{
    protected override void Start() => Tasks.Run(Rain);

    protected override void Restore() => Tasks.Run(Rain);

    private async GameTask Rain(TaskContext ctx)
    {
        while (true)
        {
            await ctx.Seconds(intervalSeconds);
            dropBox();
        }
    }
}
