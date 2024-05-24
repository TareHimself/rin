// See https://aka.ms/new-console-template for more information
using aerox.Runtime;
using WidgetTest;

Runtime.EnsureDependencies<WidgetTestModule>();
Runtime.Instance.Run();
Console.WriteLine("Hello, World!");