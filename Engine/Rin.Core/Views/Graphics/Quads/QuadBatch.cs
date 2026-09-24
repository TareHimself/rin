using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics.Quads;

public class QuadBatch : IBatch
{
    private readonly List<Quad> _quads = [];
    private readonly HashSet<ResourceHandle> _textures = [];

    public ulong GetMemoryNeeded()
    {
        return Utils.ByteSizeOf<Quad>(_quads.Count);
    }

    public IBatcher GetBatcher()
    {
        return IViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public void AddFromCommand(ICommand command)
    {
        if (command is not QuadDrawCommand asQuadDraw) return;
        _quads.AddRange(asQuadDraw.GetQuads());
        _textures.UnionWith(asQuadDraw.GetTextures());
    }

    public void DeclareResources(IGraphConfig config)
    {
        foreach (var texture in _textures)
        {
            var id = config.AddExternalImage(texture);
            Debug.Assert(id != 0, "Texture was freed while a view still references it");
            if (id != 0) config.ReadTexture(id, ImageLayout.ShaderReadOnly);
        }
    }

    public List<Quad> GetQuads()
    {
        return _quads;
    }
}