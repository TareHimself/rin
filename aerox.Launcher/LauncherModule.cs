using aerox.Runtime;
using aerox.Runtime.Widgets;
using aerox.Runtime.Windows;

namespace aerox.Launcher;

[RuntimeModule(typeof(SWidgetsModule),typeof(SWindowsModule))]
public class LauncherModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        
    }
}