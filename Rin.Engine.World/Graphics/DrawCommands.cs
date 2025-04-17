using JetBrains.Annotations;

namespace Rin.Engine.World.Graphics;

public class DrawCommands
{
    [PublicAPI] public readonly List<SkinnedMeshInfo> SkinnedMeshes = [];
    [PublicAPI] public readonly List<StaticMeshInfo> StaticMeshes = [];

    [PublicAPI] public readonly List<LightInfo> Lights = [];

    public DrawCommands AddLight(LightInfo lightInfo)
    {
        Lights.Add(lightInfo);
        return this;
    }

    public DrawCommands AddCommand(StaticMeshInfo command)
    {
        StaticMeshes.Add(command);
        return this;
    }
    
    public DrawCommands AddCommand(SkinnedMeshInfo command)
    {
        SkinnedMeshes.Add(command);
        return this;
    }
}