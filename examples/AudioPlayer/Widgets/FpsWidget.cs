using rin.Core;
using rin.Graphics;
using rin.Widgets;
using rin.Widgets.Content;
using rin.Widgets.Events;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;
using MathNet.Numerics;

namespace AudioPlayer.Widgets;


public class FpsWidget : TextWidget
{
    
    class GetStatsCommand(FpsWidget widget) : CustomCommand
    {
        public override void Run(WidgetFrame frame, uint stencilMask, ulong memory = 0)
        {
            if (frame.Surface is WindowSurface asWindowSurface)
            {
                var renderer = asWindowSurface.GetRenderer();
                widget.Content = $"""
                                  STATS
                                  {frame.BatchedDraws} Batches
                                  {frame.NonBatchedDraws} Draws
                                  {frame.StencilDraws} Stencil Draws
                                  {frame.NonDraws} Non Draws
                                  {(1.0 / SRuntime.Get().GetLastDeltaSeconds()).Round(2)} FPS
                                  {(renderer.LastFrameTime * 1000).Round(2)}ms
                                  """;
            }
            
        }

        public override bool WillDraw => false;
        public override CommandStage Stage => CommandStage.Late;
    }
    
    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        drawCommands.Add(new GetStatsCommand(this));
        base.CollectContent(info, drawCommands);
    }
}