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

    private readonly Averaged<int> _averageFps = new(0, 300);
    private readonly Dispatcher _dispatcher = new();

    public FpsView()
    {
        FontSize = 30;
    }


    public override void CollectContent(Matrix4x4 transform, PassCommands commands)
    {
        _averageFps.Add((int)Math.Round(1.0 / SEngine.Get().GetLastDeltaSeconds()));

        commands.Add(new GetStatsCommand(this));

        base.CollectContent(transform, commands);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        _dispatcher.DispatchPending();
    }

    private class GetStatsCommand : UtilityCommand
    {
        private readonly FpsView _view;

        public GetStatsCommand(FpsView view)
        {
            _view = view;
        }

        public override CommandStage Stage => CommandStage.Before;

        public override void BeforeAdd(IGraphBuilder builder)
        {
        }

        public override void Configure(IGraphConfig config)
        {
        }

        public override void Execute(ViewsFrame frame)
        {
            if (_view.Surface is WindowSurface asWindowSurface &&
                asWindowSurface.GetRenderer() is WindowRenderer asWindowRenderer)
                _view._dispatcher.Enqueue(() =>
                {
                    _view._averageCollectTime.Add(asWindowRenderer.LastCollectElapsedTime);
                    _view._averageExecuteTime.Add(asWindowRenderer.LastExecuteElapsedTime);
                    var position = frame.Surface.GetCursorPosition();
                    _view.Content = $"""
                                     STATS
                                     {new Vector2<int>((int)position.X, (int)position.Y)} Cursor Position
                                     {frame.Stats.InitialCommandCount} Initial Commands
                                     {frame.Stats.FinalCommandCount} Final Commands
                                     {frame.Stats.BatchedDrawCommandCount} Batches
                                     {frame.Stats.NonBatchedDrawCommandCount} Draws
                                     {frame.Stats.StencilWriteCount} Stencil Writes
                                     {frame.Stats.CustomCommandCount} Non Draws
                                     {frame.Stats.MemoryAllocatedBytes} Bytes Allocated
                                     {(int)_view._averageFps} Average Frames Per Second
                                     {float.Round(1 / (float)_view._averageFps * 1000.0f, 2)}ms Average Frame Time 
                                     {float.Round((float)_view._averageExecuteTime * 1000.0f, 2)}ms Execute Time 
                                     {float.Round((float)_view._averageCollectTime * 1000.0f, 2)}ms Collect Time 
                                     """;
                });
        }
    }
}