using MathNet.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Views.Content;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Commands;

namespace AudioPlayer.Views;


public class FpsView : TextBox
{
    private readonly Averaged<int> _averageFps = new Averaged<int>(0,300);
    class GetStatsCommand(FpsView view) : UtilityCommand
    {
        public override void BeforeAdd(IGraphBuilder builder)
        {
            
        }

        public override void Configure(IGraphConfig config)
        {
           
        }

        public override CommandStage Stage => CommandStage.Before;
        public override void Execute(ViewsFrame frame)
        {
            if (frame.Surface is WindowSurface asWindowSurface)
            {
                var renderer = asWindowSurface.GetRenderer();
                view.Content = $"""
                                  STATS
                                  {frame.Surface.Stats.InitialCommandCount} Initial Commands
                                  {frame.Surface.Stats.FinalCommandCount} Final Commands
                                  {frame.Surface.Stats.BatchedDrawCommandCount} Batch Commands
                                  {frame.Surface.Stats.NonBatchedDrawCommandCount} Draws
                                  {frame.Surface.Stats.StencilWriteCount} Stencil Writes
                                  {frame.Surface.Stats.CustomCommandCount} Non Draws
                                  {frame.Surface.Stats.MemoryAllocatedBytes} Bytes Allocated
                                  {(int)view._averageFps} FPS
                                  {((1 / (float)view._averageFps) * 1000.0f).Round(2)}ms
                                  """;
            }
        }
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        _averageFps.Add((int)Math.Round((1.0 / SRuntime.Get().GetLastDeltaSeconds())));
        commands.Add(new GetStatsCommand(this));
        base.CollectContent(transform, commands);
    }
}