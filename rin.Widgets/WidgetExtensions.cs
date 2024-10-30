using rin.Widgets.Animation;

namespace rin.Widgets;

public static class WidgetExtensions
{
    public static string RunAction(this Widget target, AnimationAction animation) =>
        SWidgetsModule.Get().RunAnimation(target, animation);
    
}