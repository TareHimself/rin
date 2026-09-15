using System.Numerics;
using experiment.StencilAndCover.Rendering;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;
using Examples.Common;

namespace experiment.StencilAndCover;

// Demonstrates GPU stencil-and-cover vector path fill (see Rendering/) through Rin's
// View/CommandList pipeline: a concave star (tests boundary-AA at sharp corners) and a
// two-contour "ring" shape (tests nonzero-winding hole cutting via opposite contour direction).
public class StencilAndCoverApplication : ExampleApplication
{
    protected override void OnStartup()
    {
        IGraphicsModule.Get().OnWindowCreated += window => { window.OnClose += _ => RequestExit(); };

        IViewsModule.Get().OnSurfaceCreated += SetupDemo;

        IGraphicsModule.Get()
            .CreateWindow("Stencil-and-Cover Vector Fill", new Extent2D(900, 600),
                WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown() { }

    private static void SetupDemo(IWindowSurface surface)
    {
        var star = BuildStarPath();
        var ring = BuildRingPath();

        var canvas = new CanvasView
        {
            Paint = (canvasView, _, cmds) =>
            {
                var size = canvasView.GetContentSize();

                cmds.FillPath(star, Matrix4x4.CreateTranslation(size.X * 0.3f, size.Y * 0.4f, 0f),
                    new Vector4(1f, 0.4f, 0.4f, 1f));

                cmds.FillPath(ring, Matrix4x4.CreateTranslation(size.X * 0.65f, size.Y * 0.3f, 0f),
                    new Vector4(0.5f, 0.8f, 1f, 1f));
            }
        };

        surface.Add(canvas);
    }

    private static ShapePath BuildStarPath()
    {
        const int points = 5;
        const float outerRadius = 90f;
        const float innerRadius = 36f;

        var vertices = new Vector2[points * 2];
        for (var i = 0; i < points * 2; i++)
        {
            var angle = -MathF.PI / 2f + i * MathF.PI / points;
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            vertices[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
        }

        var path = new ShapePath();
        path.MoveTo(vertices[0]);
        for (var i = 1; i < vertices.Length; i++) path.LineTo(vertices[i]);
        path.Close();
        return path;
    }

    // Outer square plus an oppositely-wound inner square: nonzero winding cancels to 0 in the
    // overlap, cutting a hole with no special-casing beyond the two contours' opposite direction.
    private static ShapePath BuildRingPath()
    {
        var path = new ShapePath();

        path.MoveTo(new Vector2(-90, -90));
        path.LineTo(new Vector2(90, -90));
        path.LineTo(new Vector2(90, 90));
        path.LineTo(new Vector2(-90, 90));
        path.Close();

        path.MoveTo(new Vector2(-40, -40));
        path.LineTo(new Vector2(-40, 40));
        path.LineTo(new Vector2(40, 40));
        path.LineTo(new Vector2(40, -40));
        path.Close();

        return path;
    }
}
