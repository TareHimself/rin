using Examples.Common;
using Rin.Core.Graphics;

namespace Examples.NodeGraphTest;

public sealed class NodeGraphExample : Example
{
    public override string Name => "node-graph";

    public override string Title => "Node Graph";

    public override Extent2D WindowSize => new(500);

    public override void Start(ExampleContext context)
    {
        context.Surface.Add(new GraphView());
    }
}
