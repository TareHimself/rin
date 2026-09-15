using System.Numerics;
using experiment.Slug.Rendering;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;
using Examples.Common;
using SixLabors.Fonts;

namespace experiment.Slug;

// Demonstrates SLUG GPU vector rendering (see Rendering/) through Rin's View/CommandList
// pipeline: a CanvasView draws both glyph outlines (via SixLabors.Fonts, used purely as an
// offline outline source) and a hand-authored non-glyph VectorPath, all rasterized by the same
// SLUG shader.
public class SlugApplication : ExampleApplication
{
    // Common Windows/Linux font family names to try, in order, before falling back to
    // whatever the first installed system family happens to be — keeps the demo from being
    // pinned to one machine's font set.
    private static readonly string[] PreferredFontFamilies = ["Segoe UI", "Arial", "Noto Sans", "DejaVu Sans"];

    protected override void OnStartup()
    {
        IGraphicsModule.Get().OnWindowCreated += window => { window.OnClose += _ => RequestExit(); };

        IViewsModule.Get().OnSurfaceCreated += SetupDemo;

        IGraphicsModule.Get()
            .CreateWindow("SLUG Vector Rendering", new Extent2D(900, 600), WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown() { }

    private void SetupDemo(IWindowSurface surface)
    {
        var font = ResolveFont(64);
        var atlas = new SlugAtlas();
        var starId = atlas.AddPath(BuildStarPath());

        var canvas = new CanvasView
        {
            Paint = (canvasView, _, cmds) =>
            {
                var size = canvasView.GetContentSize();

                cmds.AddVectorText(atlas, "Hello, SLUG!", font,
                    new Vector2(size.X * 0.1f, size.Y * 0.3f), new Vector4(1f, 0.9f, 0.5f, 1f));

                cmds.AddVectorText(atlas, "Vector rendering via Rin views", font,
                    new Vector2(size.X * 0.1f, size.Y * 0.55f), new Vector4(0.6f, 0.85f, 1f, 1f), scale: 0.4f);

                cmds.AddVectorPath(atlas, starId, new Vector2(size.X * 0.75f, size.Y * 0.35f), 1.5f,
                    new Vector4(1f, 0.4f, 0.4f, 1f));
            }
        };

        surface.Add(canvas);
    }

    // Resolves a SixLabors.Fonts.Font directly from the OS font collection. This is
    // intentionally independent of Rin's own (HarfBuzz-based) font manager — SLUG's outline
    // extraction only needs raw glyph geometry, never Rin's text-layout system.
    private static Font ResolveFont(float size)
    {
        foreach (var name in PreferredFontFamilies)
            if (SystemFonts.Collection.TryGet(name, out var family))
                return family.CreateFont(size, FontStyle.Regular);

        var fallback = SystemFonts.Collection.Families.First();
        return fallback.CreateFont(size, FontStyle.Regular);
    }

    // A hand-authored, non-glyph VectorPath — a five-point star — showing that SLUG renders
    // arbitrary vector art, not just font outlines. Edges are straight lines encoded as
    // degenerate quadratics (control point duplicated as the end point), per the Slug README.
    private static VectorPath BuildStarPath()
    {
        const int points = 5;
        const float outerRadius = 60f;
        const float innerRadius = 24f;

        var vertices = new Vector2[points * 2];
        for (var i = 0; i < points * 2; i++)
        {
            var angle = -MathF.PI / 2f + i * MathF.PI / points;
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            vertices[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }

        var curves = new QuadraticBezier[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
            curves[i] = QuadraticBezier.Line(vertices[i], vertices[(i + 1) % vertices.Length]);

        return VectorPath.FromCurves(curves);
    }
}
