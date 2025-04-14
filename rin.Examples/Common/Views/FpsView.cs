using System.Numerics;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Math;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;

namespace rin.Examples.Common.Views;

public class FpsView : TextBox
{
    private readonly Averaged<double> _averageCollectTime = new(0, 300);
    private readonly Averaged<double> _averageExecuteTime = new(0, 300);
    private readonly Averaged<double> _averageFps = new(0, 300);
    private readonly Dispatcher _dispatcher = new();

    public FpsView()
    {
        FontSize = 30;
    }


    public override void CollectContent(Matrix4x4 transform, PassCommands commands)
    {
        _averageFps.Add(1.0 / SEngine.Get().GetLastDeltaSeconds());

        commands.Add(new GetStatsCommand(this));

        base.CollectContent(transform, commands);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _dispatcher.DispatchPending();
    }

    private class GetStatsCommand(FpsView view) : UtilityCommand
    {
        public override CommandStage Stage => CommandStage.Before;

        public override void BeforeAdd(IGraphBuilder builder)
        {
        }

        public override void Configure(IGraphConfig config)
        {
        }

        public override void Execute(ViewsFrame frame)
        {
            if (view.Surface is WindowSurface asWindowSurface &&
                asWindowSurface.GetRenderer() is WindowRenderer asWindowRenderer)
                view._dispatcher.Enqueue(() =>
                {
                    view._averageCollectTime.Add(asWindowRenderer.LastCollectElapsedTime);
                    view._averageExecuteTime.Add(asWindowRenderer.LastExecuteElapsedTime);
                    var position = frame.Surface.GetCursorPosition();
                    view.Content = $"""
                                     STATS
                                     {new Vector2<int>((int)position.X, (int)position.Y)} Cursor Position
                                     {frame.Stats.InitialCommandCount} Initial Commands
                                     {frame.Stats.FinalCommandCount} Final Commands
                                     {frame.Stats.BatchedDrawCommandCount} Batches
                                     {frame.Stats.NonBatchedDrawCommandCount} Draws
                                     {frame.Stats.StencilWriteCount} Stencil Writes
                                     {frame.Stats.CustomCommandCount} Non Draws
                                     {frame.Stats.MemoryAllocatedBytes} Bytes Allocated
                                     {float.Round(1 / (float)view._averageFps * 1000.0f, 2)}ms Tick Time 
                                     {float.Round((float)view._averageExecuteTime * 1000.0f, 2)}ms Execute Time 
                                     {float.Round((float)view._averageCollectTime * 1000.0f, 2)}ms Collect Time 
                                     """;
                });
        }
    }
}