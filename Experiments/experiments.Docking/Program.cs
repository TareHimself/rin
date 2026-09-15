using experiments.Docking;
using experiments.Docking.Demo;

if (args.Length > 0 && args[0] == "test")
{
    Environment.Exit(DockTreeSmokeTest.Run());
    return;
}

using var app = new DockingApplication();
app.Run();
