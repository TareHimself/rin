using MathNet.Numerics;
using rin.Core;
using rin.Core.Math;
using rin.Graphics;
using rin.Widgets.Content;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Commands;

namespace WidgetTest;


public class FpsWidget : TextBox
{
    public Averaged<int> AverageFPS = new Averaged<int>(0,30);
    class GetStatsCommand(FpsWidget widget) : CustomCommand
    {
        public override void Run(WidgetFrame frame, uint stencilMask,DeviceBuffer.View? view = null)
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
                                  {(int)widget.AverageFPS} FPS
                                  {(renderer.LastFrameTime * 1000).Round(2)}ms
                                  """;
            }
            
        }

        public override bool WillDraw => false;
        public override CommandStage Stage => CommandStage.Late;
    }
    
    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        AverageFPS.Add((int)Math.Round((1.0 / SRuntime.Get().GetLastDeltaSeconds())));
        drawCommands.Add(new GetStatsCommand(this));
        base.CollectContent(info, drawCommands);
    }
}