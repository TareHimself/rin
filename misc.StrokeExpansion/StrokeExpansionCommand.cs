using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.CommandHandlers;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace misc.StrokeExpansion;

public class StrokeExpansionHandler : ICommandHandler
{
    private StrokeExpansionCommand[] _commands = [];
    private IComputeShader _shader = IGraphicsModule.Get().MakeCompute("StrokeExpansion/stroke_expansion.slang");

    public void Init(ICommand[] commands)
    {
        _commands = commands.Cast<StrokeExpansionCommand>().ToArray();
    }

    public void Configure(IPassConfig passConfig, SurfaceContext surfaceContext, IGraphConfig config)
    {
    }

    public void Execute(IPassConfig passConfig,
        SurfaceContext surfaceContext, ICompiledGraph graph, IExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}

public class StrokeExpansionCommand : TCommand<MainPassConfig, StrokeExpansionHandler>
{
}