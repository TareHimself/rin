using Rin.Core.Animation;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Animation;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace rin.Examples.ViewsTest;

public class TestAnimationSizerView : SizerView
{
    private float _width;

    public TestAnimationSizerView()
    {
        _width = WidthOverride.GetValueOrDefault(0);
    }

    protected override void OnAddedToSurface(ISurface surface)
    {
        base.OnAddedToSurface(surface);
        _width = WidthOverride.GetValueOrDefault(0);
    }

    protected override void OnCursorEnter(CursorMoveSurfaceEvent e)
    {
        this.StopAll().WidthTo(HeightOverride.GetValueOrDefault(0), easingFunction: EasingFunctions.EaseInOutCubic);
    }

    protected override void OnCursorLeave()
    {
        this.StopAll().WidthTo(_width, easingFunction: EasingFunctions.EaseInOutCubic);
    }
}