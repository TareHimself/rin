using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Window;
using Examples.RenderGraphOverlay.Snapshot;
using Examples.RenderGraphOverlay.Views;

namespace Examples.RenderGraphOverlay;

public static class GraphOverlay
{
    public static OverlayView? Attach(ISurface surface, bool startOpen = false)
    {
        return Attach(surface, new Vector2(1f), startOpen);
    }

    public static OverlayView? Attach(ISurface surface, Vector2 corner, bool startOpen = false)
    {
        if (surface is not IWindowSurface windowSurface) return null;

        return surface.Add(new OverlayView(new GraphSnapshotService(windowSurface.Renderer), corner, startOpen));
    }

    public static OverlayView? Attach(IWindowRenderer renderer, bool startOpen = false)
    {
        return IViewsModule.Get().GetWindowSurface(renderer) is { } surface ? Attach(surface, startOpen) : null;
    }
}
