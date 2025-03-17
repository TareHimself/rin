using MathNet.Numerics;
using Rin.Engine.Core;
using Rin.Engine.Core.Math;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Commands;

namespace rin.Examples.Common.Views;


public class FpsView : TextBox
{
    
    private readonly Averaged<int> _averageFps = new Averaged<int>(0,300);
    private readonly Averaged<double> _averageCollectTime = new Averaged<double>(0,300);
    private readonly Averaged<double> _averageExecuteTime = new Averaged<double>(0,300);
    private Dispatcher _dispatcher = new Dispatcher();
    
    public FpsView()
    {
        FontSize = 30;
    }
    class GetStatsCommand : UtilityCommand
    {
        private readonly FpsView _view;

        public GetStatsCommand(FpsView view)
        {
            _view = view;
        }

        public override void BeforeAdd(IGraphBuilder builder)
        {
            
        }

        public override void Configure(IGraphConfig config)
        {
            
        }

        public override CommandStage Stage => CommandStage.Before;
        public override void Execute(ViewsFrame frame)
        {
            if (_view.Surface is WindowSurface asWindowSurface && asWindowSurface.GetRenderer() is WindowRenderer asWindowRenderer)
            {
                _view._dispatcher.Enqueue(() =>
                {
                    _view._averageCollectTime.Add(asWindowRenderer.LastCollectElapsedTime);
                    _view._averageExecuteTime.Add(asWindowRenderer.LastExecuteElapsedTime);
                    var position = frame.Surface.GetCursorPosition();
                    _view.Content = $"""
                                     STATS
                                     {new Vector2<int>((int)position.X,(int)position.Y)} Cursor Position
                                     {frame.Stats.InitialCommandCount} Initial Commands
                                     {frame.Stats.FinalCommandCount} Final Commands
                                     {frame.Stats.BatchedDrawCommandCount} Batches
                                     {frame.Stats.NonBatchedDrawCommandCount} Draws
                                     {frame.Stats.StencilWriteCount} Stencil Writes
                                     {frame.Stats.CustomCommandCount} Non Draws
                                     {frame.Stats.MemoryAllocatedBytes} Bytes Allocated
                                     {(int)_view._averageFps} Average Frames Per Second
                                     {((1 / (float)_view._averageFps) * 1000.0f).Round(2)}ms Average Frame Time 
                                     {(_view._averageExecuteTime  * 1000.0f).Round(2)}ms Execute Time 
                                     {(_view._averageCollectTime  * 1000.0f).Round(2)}ms Collect Time 
                                     """;
                });
                
            }
        }
    }
    

    public override void CollectContent(Mat3 transform, PassCommands commands)
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
}