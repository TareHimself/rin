// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using rsl.LanguageServer;
using WatsonWebserver;
using WatsonWebserver.Core;
using WatsonWebserver.Extensions.HostBuilderExtension;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

Webserver server = new HostBuilder("127.0.0.1", 8989, false, (ctx) => ctx.Response.Send("Yo"))
    .MapStaticRoute(HttpMethod.GET, Validate.Route,Validate.Run)
    .Build();

server.Start();

Console.WriteLine("Server started");
Console.ReadKey();