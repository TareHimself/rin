using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Rin.Framework.Views.Events;
using Rin.Framework.Views.Graphics;

namespace NodeGraphTest;

public class PinConnectAttemptEvent(ISurface surface,DefaultPinConnectionRequest request) : IPinConnectionAttemptEvent
{
    public ISurface Surface { get; } = surface;
    public Vector2 Position => request.Position;
    
    public IPinConnectionRequest Request { get; } = request;
    public bool ReverseTestOrder => false;
    
    [MemberNotNullWhen(true,nameof(PinView))]
    public bool Handled => PinView is not null;
    
    
    public IGraphPinView? PinView { get; set; }
}