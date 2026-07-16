using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace misc.VectorRendering.Slug;

// Push constants sent to slug.slang for each draw call.
// Must match struct SlugPush in slug.slang exactly.
// Layout: pointer (8) + ResourceHandle (4) + ResourceHandle (4) + Matrix4x4 (64) = 80 bytes.
[StructLayout(LayoutKind.Sequential)]
internal struct SlugPush
{
    // GPU virtual address of the GlyphDrawData[] instance buffer for this command.
    public required ulong BufferAddress;

    // Bindless texture handles — used via TexelLoad() in the shader.
    public required ResourceHandle CurveTexture;
    public required ResourceHandle BandTexture;

    // Orthographic projection matrix mapping screen pixels to clip space.
    public required Matrix4x4 Projection;
}

// ICommandHandler that executes all SlugCommand instances for one frame.
// Follows the same pattern as BlurFirstPassCommandHandler:
//   Init()      — cast commands to concrete type
//   Configure() — declare buffer resources for the frame graph
//   Execute()   — write instance data, bind shader, draw
public class SlugHandler : ICommandHandler
{
    // The shader is loaded once at construction time.
    // MakeGraphics() caches compiled shaders internally, so this is cheap.
    private readonly IGraphicsShader _shader =
        IGraphicsModule.Get().MakeGraphics("VectorRendering/Slug/slug.slang");

    // One buffer ID per SlugCommand — allocated fresh every frame by the graph.
    private uint[] _bufferIds = [];
    private SlugCommand[] _commands = [];

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<SlugCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
        // Create one HostThenGraphics buffer per command, sized exactly for that
        // command's instance count.  HostThenGraphics means we write CPU-side in
        // Execute() and the graph ensures it is flushed to the GPU before drawing.
        _bufferIds = _commands
            .Select(cmd => config.CreateBuffer<GlyphDrawData>(
                Math.Max(1, cmd.Draws.Count), GraphBufferUsage.HostThenGraphics))
            .ToArray();
    }

    public void Execute(
        IPassConfig      passConfig,
        SurfaceContext   surfaceContext,
        ICompiledGraph   graph,
        IExecutionContext ctx)
    {
        foreach (var (cmd, bufferId) in _commands.Zip(_bufferIds))
        {
            if (cmd.Draws.Count == 0) continue;

            // Lazy GPU upload: if any shapes were added to the atlas since last frame,
            // re-upload the curve and band textures before reading from them.
            cmd.Atlas.EnsureUploaded();

            var buffer = graph.GetBufferOrException(bufferId);

            // Write all GlyphDrawData instances for this command into the per-frame buffer.
            // DeviceBufferView.Write(List<T>) uses the list's internal span directly — no copy.
            buffer.Write(cmd.Draws);

            if (_shader.Bind(ctx) is not { } bind) continue;

            bind.Push(new SlugPush
            {
                BufferAddress = buffer.GetAddress(),
                CurveTexture  = cmd.Atlas.CurveHandle,
                BandTexture   = cmd.Atlas.BandHandle,
                Projection    = surfaceContext.ProjectionMatrix
            })
            // 6 vertices per quad (two triangles), one instance per shape.
            // The vertex shader uses SV_InstanceID to index into the buffer.
            .Draw(6, (uint)cmd.Draws.Count);
        }
    }
}
