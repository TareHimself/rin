using Rin.Framework.Animation;
using Rin.Framework.Shared.Math;
using Rin.Framework.Views.Animation;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;

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