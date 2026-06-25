using System.Numerics;
using Rin.Core;
using Rin.Core.Views;

namespace NodeGraphTest;

/// <summary>
/// </summary>
public interface IGraphPinView : IView, IJsonSerializable
{
    public string Name { get; set; }
    public IGraphNodeView? ParentNode { get; set; }

    public PinType PinType { get; }

    public Vector2 GetPinAbsolutePosition();
}