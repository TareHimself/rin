using System.Text.Json;
using Newtonsoft.Json;
using WatsonWebserver.Core;

namespace rsl.LanguageServer;

public static class Extensions
{
    public static async Task<bool> SendJson<T>(this HttpResponseBase resp, T data)
    {

        await resp.Send(await Task.Run(() => JsonConvert.SerializeObject(data, Formatting.Indented)));
        return false;
    }
}