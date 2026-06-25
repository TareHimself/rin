using System.Numerics;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Window;
using Rin.Core.Views.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views.Font;
using rin.Examples.Common;
using misc.VectorRendering.Slug;
using SixLabors.Fonts;

namespace misc.VectorRendering;

// Entry point for the SLUG vector rendering experiment.
// Extends ExampleApplication which wires up Vulkan, ViewsModule, and Miniaudio.
//
// On startup:
//   1. Register embedded Content/VectorRendering/** resources so the shader at
//      "VectorRendering/Slug/slug.slang" is resolvable from SFramework.Sources.
//   2. Create a window and listen for the surface ready event.
//   3. Build a SlugAtlas, upload it, and drive a CanvasView to emit SlugCommands.
public class VectorRenderingApplication : ExampleApplication
{
    protected override void OnStartup()
    {
        // Register the embedded resources from this assembly under the "VectorRendering" alias.
        // Files at Content/VectorRendering/** are served as "VectorRendering/**" to the resolver.
        // This lets IGraphicsModule.MakeGraphics("VectorRendering/Slug/slug.slang") find the shader.
        SFramework.Sources.AddSource(
            AssemblyContentResource.New<VectorRenderingApplication>("VectorRendering"));

        IGraphicsModule.Get().OnWindowCreated += window =>
        {
            window.OnClose += _ => RequestExit();
        };

        IViewsModule.Get().OnSurfaceCreated += SetupSlugDemo;

        IGraphicsModule.Get()
            .CreateWindow("SLUG Vector Rendering", new Extent2D(900, 600),
                WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown() { }

    // Build a SlugAtlas with test glyphs and wire it into the surface's draw pipeline.
    // This demonstrates the general vector drawing API: we create VectorPaths from font
    // glyphs and draw them with the SLUG GPU algorithm.
    private void SetupSlugDemo(IWindowSurface surface)
    {
        // The ViewsModule pre-loads "Noto Sans" at startup via SixLaborsFontManager.
        // GetFont returns an IFont wrapping a SixLabors FontFamily.
        var fontManager = IViewsModule.Get().FontManager;
        if (fontManager.GetFont("Noto Sans") is not SixLaborsFont rinFont) return;

        // Create a SixLabors Font at 64pt — the size used for glyph extraction.
        // SlugAtlas.GetOrAddGlyph expects a SixLabors.Fonts.Font so we can run
        // TextRenderer.RenderTextTo() on it to extract bezier outlines.
        var font = rinFont.Family.CreateFont(64, FontStyle.Regular);

        // The atlas is lazy: GetOrAddGlyph and EnsureUploaded are called by AddVectorText
        // and SlugHandler.Execute() automatically. We just need to create the atlas here
        // so it lives for the lifetime of the surface.
        var atlas = new SlugAtlas();

        // A CanvasView gives us a CommandList to add SLUG draws into every frame.
        // Paint() is called during surface collection, so it runs on the update thread.
        var canvas = new CanvasView
        {
            Paint = (canvasView, _, cmds) =>
            {
                var size    = canvasView.GetContentSize();
                var textPos = new Vector2(size.X * 0.1f, size.Y * 0.4f);

                // AddVectorText batches all glyphs into a single SlugCommand → one GPU draw call.
                cmds.AddVectorText(atlas, "Hello World, My name is slim shady", font, textPos,
                    color: new Vector4(1f, 0.9f, 0.5f, 1f));
            }
        };

        surface.Add(canvas);
    }
}
