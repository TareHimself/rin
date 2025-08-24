using JetBrains.Annotations;
using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Engine.World.Graphics;

public class CommandList()
{
    [PublicAPI] public readonly List<LightInfo> Lights = [];
    [PublicAPI] public readonly List<SkinnedMeshInfo> SkinnedMeshes = [];
    [PublicAPI] public readonly List<StaticMeshInfo> StaticMeshes = [];
    [PublicAPI] public readonly List<uint> ExplicitPasses = [];

    
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

    /// <summary>
    /// Add the id of a pass that must be executed before the mesh passes
    /// </summary>
    /// <param name="pass"></param>
    /// <returns></returns>
    // public CommandList AddPass(uint pass)
    // {
    //
    // }
}