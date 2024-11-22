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


        this.Test(5,"1").After().Test(5,"2");
    }

    public AnimationRunner AnimationRunner { get; init; } = new AnimationRunner();
}