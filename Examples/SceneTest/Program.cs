// See https://aka.ms/new-console-template for more information

using SceneTest;

if (LogFileRedirector.GetLogFilePath(args) is { } logFilePath) LogFileRedirector.RedirectTo(logFilePath);

using var app = new SceneTestApplication();
app.Run();
