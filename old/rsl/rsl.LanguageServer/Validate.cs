using System.Text.Json;
using System.Text.Json.Serialization;
using rsl.Parser;
using Newtonsoft.Json;
using WatsonWebserver.Core;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace rsl.LanguageServer;

public class Validate : IRoute
{
    
    public class Payload
    {
        [JsonPropertyName("success")]
        public string Data { get; set; }
    }
    
    public class Response
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("symbols")] public string[] Symbols { get; set; } = [];

        public Response(bool success)
        {
            Success = success;
        }
    }
    
    public static string Route => "/validate";

    public static async Task Run(HttpContextBase ctx)
    {
        try
        {
            using TextReader reader = new StreamReader(ctx.Request.Data);
            var data = JsonConvert.DeserializeObject<Payload>(await reader.ReadToEndAsync());

            if (data == null)
            {
                await ctx.Response.SendJson(new Response(false));
                return;
            }

            var tokenizer = new Tokenizer.Tokenizer();
            var parser = new Parser.Parser();
            
            var tokens = Path.Exists(data.Data)
                ? tokenizer.Run(data.Data)
                : tokenizer.Run(data.Data, "<rsl>");
            
            var ast = parser.Run(tokens);
            
            ast = ast.ResolveIncludes((node, module) =>
            {
                if (Path.IsPathRooted(node.File)) return Path.GetFullPath(node.File);

                return Path.GetFullPath(node.File,
                    Directory.GetParent(node.SourceFile)?.FullName ?? Directory.GetCurrentDirectory());
            }, tokenizer, parser);
        
            ast.ResolveStructReferences();
            
            await ctx.Response.SendJson(ast);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await ctx.Response.SendJson(new Response(false));
            //throw;
        }
    
        await ctx.Response.Send("Hello from default route!"); 
    }
}