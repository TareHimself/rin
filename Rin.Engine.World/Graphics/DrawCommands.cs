using JetBrains.Annotations;

namespace Rin.Engine.World.Graphics;

public class DrawCommands
{
    [PublicAPI] public readonly List<GeometryInfo> GeometryCommands = [];

    [PublicAPI] public readonly List<LightInfo> Lights = [];

    public DrawCommands AddLight(LightInfo lightInfo)
    {
        Lights.Add(lightInfo);
        return this;
    }

    public DrawCommands AddCommand(GeometryInfo command)
    {
        GeometryCommands.Add(command);
        return this;
    }
}