using JetBrains.Annotations;

namespace Rin.Engine.World.Graphics;

public class CommandList
{
    [PublicAPI] public readonly List<SkinnedMeshInfo> SkinnedMeshes = [];
    [PublicAPI] public readonly List<StaticMeshInfo> StaticMeshes = [];

    [PublicAPI] public readonly List<LightInfo> Lights = [];

    public CommandList AddLight(LightInfo lightInfo)
    {
        Lights.Add(lightInfo);
        return this;
    }

    public CommandList AddStatic(StaticMeshInfo command)
    {
        StaticMeshes.Add(command);
        return this;
    }
    
    public CommandList AddSkinned(SkinnedMeshInfo command)
    {
        SkinnedMeshes.Add(command);
        return this;
    }
}