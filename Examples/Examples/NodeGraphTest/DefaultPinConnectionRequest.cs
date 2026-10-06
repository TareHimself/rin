using System.Numerics;

namespace Examples.NodeGraphTest;

public class DefaultPinConnectionRequest(IGraphPinView pin, Vector2 from) : IPinConnectionRequest
{
    public Vector2 From { get; set; } = from;
    public Vector2 Position { get; set; } = from;

    public IGraphPinView Requester { get; } = pin;
}