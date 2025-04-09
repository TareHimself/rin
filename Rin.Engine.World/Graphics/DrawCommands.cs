using JetBrains.Annotations;

namespace Rin.Engine.World.Graphics;

public class DrawCommands
{
    [PublicAPI] public readonly List<StaticMeshInfo> GeometryCommands = [];

    [PublicAPI] public readonly List<LightInfo> Lights = [];

    public DrawCommands AddLight(LightInfo lightInfo)
    {
        Lights.Add(lightInfo);
        return this;
    }

    public DrawCommands AddCommand(StaticMeshInfo command)
    {
        GeometryCommands.Add(command);
        return this;
    }
}