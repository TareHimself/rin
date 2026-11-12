using System.Numerics;
using Rin.Core.Shared;
using Rin.Core.Shared.Math;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics;

namespace Examples.Common.Views;

public class FpsView : TextBoxView
{
    private static readonly uint NumAveragedSamples = 100;
    private readonly Averaged<double> _averageFps = new(0, NumAveragedSamples);
    private readonly AveragedStatCategory _collectTime = new("Engine.Collect");
    private readonly AveragedStatCategory _graphBuildTime = new("Engine.Rendering.Graph.Build");
    private readonly AveragedStatCategory _graphCompileTime = new("Engine.Rendering.Graph.Compile");
    private readonly AveragedStatCategory _graphExecuteTime = new("Engine.Rendering.Graph.Execute");
    private readonly AveragedStatCategory _renderTime = new("Engine.Rendering");
    private readonly AveragedStatCategory _updateTime = new("Engine.Update");

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
        //_averageFps.Add(1.0 / SEngine.Get().GetLastDeltaSeconds());
        _updateTime.Update();
        _collectTime.Update();
        _renderTime.Update();
        _graphBuildTime.Update();
        _graphCompileTime.Update();
        _graphExecuteTime.Update();
        Content = $"""
                   [Main Thread]
                   {_updateTime.GetMilliseconds()}ms Update
                   {_collectTime.GetMilliseconds()}ms Collect

                   [Render Thread]
                   {_graphBuildTime.GetMilliseconds()}ms Graph Build
                   {_graphCompileTime.GetMilliseconds()}ms Graph Compile
                   {_graphExecuteTime.GetMilliseconds()}ms Graph Execute
                   {_renderTime.GetMilliseconds()}ms Total
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
}
