using rin.Core;
using rin.Core.Animation;

namespace Animations;

[RuntimeModule]
public class AnimationTestModule : RuntimeModule, IAnimatable
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        
        runtime.OnTick += (_) => ((IAnimatable)this).Update();


        this.Delay(5).Do(() => Console.WriteLine("Five seconds have passed")).Delay(5).Do(() => Console.WriteLine("Five more seconds have passed"));
    }

    public AnimationRunner AnimationRunner { get; init; } = new AnimationRunner();
}