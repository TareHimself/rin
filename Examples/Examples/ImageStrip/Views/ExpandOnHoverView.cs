using Rin.Core.Animation;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Animation;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Events;
using Rin.Core.Views.Graphics;

namespace Examples.ImageStrip.Views;

public class ExpandOnHoverView : SizerView
{
    private float _width;

    public ExpandOnHoverView()
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