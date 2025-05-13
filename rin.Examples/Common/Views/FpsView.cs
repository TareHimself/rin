using System.Numerics;
using Rin.Engine;
using Rin.Engine.Math;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Graphics;

namespace rin.Examples.Common.Views;

public class FpsView : TextBox
{
    private static readonly uint NumAveragedSamples = 30;
    private readonly Averaged<double> _averageFps = new(0, NumAveragedSamples);
    private readonly AveragedStatCategory _collectTime = new("Engine.Collect");
    private readonly Dispatcher _dispatcher = new();
    private readonly AveragedStatCategory _graphCompileTime = new("Engine.Rendering.Graph.Compile");
    private readonly AveragedStatCategory _graphExecuteTime = new("Engine.Rendering.Graph.Execute");
    private readonly AveragedStatCategory _renderTime = new("Engine.Rendering");

    public FpsView()
    {
        FontSize = 30;
    }


    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        //commands.Add(new GetStatsCommand(this));
        base.CollectContent(transform, commands);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        var position = Surface?.GetCursorPosition() ?? Vector2.Zero;
        _averageFps.Add(1.0 / SEngine.Get().GetLastDeltaSeconds());
        _collectTime.Update();
        _renderTime.Update();
        _graphCompileTime.Update();
        _graphExecuteTime.Update();
        Content = $"""
                   STATS
                   {new Vector2<int>((int)position.X, (int)position.Y)} Cursor Position
                   {float.Round(1 / (float)_averageFps * 1000.0f, 2)}ms Tick Time 
                   {float.Round(1f / (_renderTime.GetMilliseconds() / 1000f))} FPS
                   {_collectTime.GetMilliseconds()}ms Collect
                   {_renderTime.GetMilliseconds()}ms Render
                   {_graphCompileTime.GetMilliseconds()}ms Graph Compile
                   {_graphExecuteTime.GetMilliseconds()}ms Graph Execute
                   """;
    }

    private class AveragedStatCategory(string statId)
    {
        private readonly Averaged<double> _stat = new(0, NumAveragedSamples);

        public void Update()
        {
            _stat.Add(Profiling.GetElapsedOrZero(statId).TotalSeconds);
        }

        public float GetMilliseconds()
        {
            return float.Round((float)(_stat * 1000.0), 2);
        }
    }

//     private class GetStatsCommand(FpsView view) : UtilityCommand
//     {
//         public override CommandStage Stage => CommandStage.Before;
//
//         public override void BeforeAdd(IGraphBuilder builder)
//         {
//         }
//
//         public override void Configure(IGraphConfig config)
//         {
//         }
//
//         public override void Execute(ViewsFrame frame)
//         {
//             if (view.Surface is WindowSurface asWindowSurface &&
//                 asWindowSurface.GetRenderer() is WindowRenderer asWindowRenderer)
//                 view._dispatcher.Enqueue(() =>
//                 {
//                     view._averageCollectTime.Add(asWindowRenderer.LastCollectElapsedTime);
//                     view._averageExecuteTime.Add(asWindowRenderer.LastExecuteElapsedTime);
//                     var position = frame.Surface.GetCursorPosition();
//                     view.Content = $"""
//                                      STATS
//                                      {new Vector2<int>((int)position.X, (int)position.Y)} Cursor Position
//                                      {frame.Stats.InitialCommandCount} Initial Commands
//                                      {frame.Stats.FinalCommandCount} Final Commands
//                                      {frame.Stats.BatchedDrawCommandCount} Batches
//                                      {frame.Stats.NonBatchedDrawCommandCount} Draws
//                                      {frame.Stats.StencilWriteCount} Stencil Writes
//                                      {frame.Stats.CustomCommandCount} Non Draws
//                                      {frame.Stats.MemoryAllocatedBytes} Bytes Allocated
//                                      {float.Round(1 / (float)view._averageFps * 1000.0f, 2)}ms Tick Time 
//                                      {float.Round((float)view._averageExecuteTime * 1000.0f, 2)}ms Execute Time 
//                                      {float.Round((float)view._averageCollectTime * 1000.0f, 2)}ms Collect Time 
//                                      """;
//                 });
//         }
//     }
}