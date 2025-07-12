using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.CommandHandlers;

/// <summary>
/// Responsible for executing commands in a pass
/// </summary>
public interface ICommandHandler
{

    public void Init(ICommand[] commands);
    /// <summary>
    /// Configure resources for this handler
    /// </summary>
    /// <param name="config"></param>
    /// <param name="passConfig"></param>
    public void Configure(IGraphConfig config,IPassConfig passConfig);
    /// <summary>
    /// Execute the commands in this handler
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="ctx"></param>
    /// <param name="passConfig"></param>
    public void Execute(ICompiledGraph graph, IExecutionContext ctx,IPassConfig passConfig);
}