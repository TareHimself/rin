using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics.Quads;

/// <summary>
///     The quad commands drawn together. It reads their quads in place, and releases them when disposed.
/// </summary>
public class QuadBatch : IBatch, IDisposable
{
    private readonly List<QuadDrawCommand> _commands = [];
    private readonly HashSet<ResourceHandle> _textures = [];

    public int QuadCount { get; private set; }

    public IReadOnlyList<QuadDrawCommand> Commands => _commands;

    public ulong GetMemoryNeeded()
    {
        return Utils.ByteSizeOf<Quad>(QuadCount);
    }

    public IBatcher GetBatcher()
    {
        return IViewsModule.Get().GetBatcher<DefaultQuadBatcher>();
    }

    public void AddFromCommand(ICommand command)
    {
        if (command is not QuadDrawCommand asQuadDraw) return;
        _commands.Add(asQuadDraw);
        QuadCount += asQuadDraw.Quads.Length;
        foreach (var texture in asQuadDraw.Textures) _textures.Add(texture);
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

    public void Dispose()
    {
        foreach (var command in _commands) command.Dispose();
        _commands.Clear();
        QuadCount = 0;
    }
}