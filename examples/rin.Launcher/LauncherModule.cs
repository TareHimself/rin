using rin.Core;
using rin.Widgets;
using rin.Windows;

namespace rin.Launcher;

[RuntimeModule(typeof(SWidgetsModule),typeof(SWindowsModule))]
public class LauncherModule : RuntimeModule
{
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        
    }
}