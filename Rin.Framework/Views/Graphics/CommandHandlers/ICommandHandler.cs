using Rin.Framework.Graphics;
using Rin.Framework.Graphics.FrameGraph;
using Rin.Framework.Views.Graphics.Commands;

namespace Rin.Framework.Views.Graphics.CommandHandlers;

/// <summary>
///     Responsible for executing commands in a pass
/// </summary>
public interface ICommandHandler
{
    public void Init(ICommand[] commands);

    /// <summary>
    ///     Configure resources for this handler
    /// </summary>
    /// <param name="passConfig"></param>
    /// <param name="surfaceContext"></param>
    /// <param name="config"></param>
    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config);

    /// <summary>
    ///     Execute the commands in this handler
    /// </summary>
    /// <param name="passConfig"></param>
    /// <param name="surfaceContext"></param>
    /// <param name="graph"></param>
    /// <param name="ctx"></param>
    public void Execute(IPassConfig passConfig, SurfaceContext surfaceContext, ICompiledGraph graph,
        IExecutionContext ctx);
}