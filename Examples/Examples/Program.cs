using Examples;
using Examples.Common;
using Examples.HeadlessTest;

if (LogFileRedirector.GetLogFilePath(args) is { } logFilePath) LogFileRedirector.RedirectTo(logFilePath);

var name = args.Length > 0 && !args[0].StartsWith('-') ? args[0] : null;

if (name == HeadlessApplication.Name)
{
    using var headless = new HeadlessApplication();
    headless.Run();
    return 0;
}

var example = name is null ? null : ExampleRegistry.Find(name);
if (name is not null && example is null)
{
    Console.Error.WriteLine($"Unknown example '{name}'. Available:");
    foreach (var known in ExampleRegistry.All) Console.Error.WriteLine($"  {known.Name}");
    Console.Error.WriteLine($"  {HeadlessApplication.Name}");
    return 1;
}

using var app = new ExampleHost(example, ExampleRegistry.All, args);
app.Run();
return 0;
