using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;
using SixLabors.Fonts;

namespace experiment.Slug.Rendering;

// Push constants sent to slug.slang for each draw call — must match struct SlugPush exactly.
[StructLayout(LayoutKind.Sequential)]
[NoReorder]
internal struct SlugPush
{
    public required ulong BufferAddress;
    public required ResourceHandle CurveTexture;
    public required ResourceHandle BandTexture;
    public required Matrix4x4 Projection;
}

// Batches one or more SLUG draws that share the same atlas into a single instanced draw call.
public class SlugDrawCommand : TCommand<MainPassConfig, SlugDrawHandler>
{
    public required SlugAtlas Atlas;
    public required List<SlugInstanceData> Instances;
}

public partial class SlugDrawHandler : ICommandHandler
{
    [GraphicsShader("Slug/slug.slang")]
    private partial IGraphicsShader Shader { get; }

    private SlugDrawCommand[] _commands = [];
    private uint[] _bufferIds = [];

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<SlugDrawCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        _bufferIds = _commands
            .Select(cmd => config.CreateBuffer<SlugInstanceData>(
                Math.Max(1, cmd.Instances.Count), GraphBufferUsage.HostThenGraphics))
            .ToArray();
    }

    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph,
        IExecutionContext ctx)
    {
        foreach (var (command, bufferId) in _commands.Zip(_bufferIds))
        {
            if (command.Instances.Count == 0) continue;

            command.Atlas.EnsureUploaded();

            var buffer = graph.GetBufferOrException(bufferId);
            buffer.Write(CollectionsMarshal.AsSpan(command.Instances));

            if (Shader.Bind(ctx) is not { } bind) continue;

            bind.Push(new SlugPush
                {
                    BufferAddress = buffer.GetAddress(),
                    CurveTexture = command.Atlas.CurveHandle,
                    BandTexture = command.Atlas.BandHandle,
                    Projection = surfaceContext.ProjectionMatrix
                })
                .Draw(6, (uint)command.Instances.Count);
        }
    }
}

// CommandList extension methods so views can add SLUG draws the same way other draw types do
// (mirrors the BlurPassExtensions.AddBlur idiom).
public static class SlugCommandExtensions
{
    public static CommandList AddVectorPath(
        this CommandList list, SlugAtlas atlas, uint shapeId, Vector2 position, float scale, Vector4 color)
    {
        var entry = atlas.GetEntry(shapeId);

        // Expand the screen quad by 1px so the shader's antialiasing kernel (which extends
        // roughly half a pixel past the exact boundary) never gets clipped at the quad edge.
        var expand = new Vector2(1f);
        var minPos = position + entry.BoundsMin * scale - expand;
        var maxPos = position + entry.BoundsMax * scale + expand;
        var minEm = entry.BoundsMin - expand / scale;
        var maxEm = entry.BoundsMax + expand / scale;

        var instance = new SlugInstanceData
        {
            MinPos = minPos,
            MaxPos = maxPos,
            MinEm = minEm,
            MaxEm = maxEm,
            Banding = new Vector4(entry.BandScale.X, entry.BandScale.Y, entry.BandOffset.X, entry.BandOffset.Y),
            ShapeLocX = entry.BandTexX,
            ShapeLocY = entry.BandTexY,
            BandMaxX = entry.BandMaxX,
            BandMaxY = entry.BandMaxY,
            Color = color
        };

        if (FindExistingCommand(list, atlas) is { } existing)
            existing.Instances.Add(instance);
        else
            list.Add(new SlugDrawCommand { Atlas = atlas, Instances = [instance] });

        return list;
    }

    public static CommandList AddVectorText(
        this CommandList list, SlugAtlas atlas, string text, Font font, Vector2 position, Vector4 color,
        float scale = 1f)
    {
        if (!TextMeasurer.TryMeasureCharacterBounds(text, new TextOptions(font), out var bounds))
            return list;

        for (var i = 0; i < text.Length && i < bounds.Length; i++)
        {
            var ch = text[i];
            if (char.IsWhiteSpace(ch)) continue;

            var shapeId = atlas.GetOrAddGlyph(font, ch);
            var glyphOffset = new Vector2(bounds[i].Bounds.X, bounds[i].Bounds.Y);
            AddVectorPath(list, atlas, shapeId, position + glyphOffset * scale, scale, color);
        }

        return list;
    }

    private static SlugDrawCommand? FindExistingCommand(CommandList list, SlugAtlas atlas)
    {
        for (var i = list.Commands.Count - 1; i >= 0; i--)
            if (list.Commands[i] is SlugDrawCommand cmd && cmd.Atlas == atlas)
                return cmd;
        return null;
    }
}
