using aerox.Runtime.Widgets.Animation;

namespace aerox.Runtime.Widgets;

public static class WidgetExtensions
{
    public static string RunAction(this Widget target, AnimationAction animation) =>
        SWidgetsModule.Get().RunAnimation(target, animation);
}