using WatsonWebserver.Core;

namespace rsl.LanguageServer;

public interface IRoute
{
    public static abstract string Route { get; }
    public abstract static Task Run(HttpContextBase ctx);
}