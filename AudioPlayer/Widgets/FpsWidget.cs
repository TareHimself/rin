using aerox.Runtime;
using aerox.Runtime.Graphics;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Content;
using aerox.Runtime.Widgets.Events;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Commands;
using MathNet.Numerics;

namespace AudioPlayer.Widgets;


public class FpsWidget : WText
{

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        if (Surface is WindowSurface asWindowSurface)
        {
            var renderer = asWindowSurface.GetRenderer();
            Content = $"""
                       {(1.0 / SRuntime.Get().GetLastDeltaSeconds()).Round(2)} FPS
                       {(renderer.LastFrameTime * 1000).Round(2)}ms
                       """;
        }
        
        base.CollectContent(info, drawCommands);
    }
}