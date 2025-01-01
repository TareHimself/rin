using MathNet.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Views.Content;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Commands;

namespace AudioPlayer.Widgets;


public class FpsWidget : TextBox
{
    private readonly Averaged<int> _averageFps = new Averaged<int>(0,300);
    class GetStatsCommand(FpsWidget widget) : CustomCommand
    {
        public override void Run(ViewsFrame frame, uint stencilMask, IDeviceBuffer? buffer = null)
        {
            if (frame.Surface is WindowSurface asWindowSurface)
            {
                var renderer = asWindowSurface.GetRenderer();
                widget.Content = $"""
                                  STATS
                                  {frame.Surface.Stats.InitialCommandCount} Initial Commands
                                  {frame.Surface.Stats.FinalCommandCount} Final Commands
                                  {frame.Surface.Stats.BatchedDrawCommandCount} Batches
                                  {frame.Surface.Stats.NonBatchedDrawCommandCount} Draws
                                  {frame.Surface.Stats.StencilWriteCount} Stencil Writes
                                  {frame.Surface.Stats.CustomCommandCount} Non Draws
                                  {frame.Surface.Stats.MemoryAllocatedBytes} Bytes Allocated
                                  {(int)widget._averageFps} FPS
                                  {((1 / (float)widget._averageFps) * 1000.0f).Round(2)}ms
                                  """;
            }
            
        }

        public override bool WillDraw => false;
        public override CommandStage Stage => CommandStage.Late;
    }

    public override void CollectContent(Mat3 transform, DrawCommands drawCommands)
    {
        _averageFps.Add((int)Math.Round((1.0 / SRuntime.Get().GetLastDeltaSeconds())));
        drawCommands.Add(new GetStatsCommand(this));
        base.CollectContent(transform, drawCommands);
    }
}