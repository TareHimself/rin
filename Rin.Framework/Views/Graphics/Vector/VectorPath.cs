using System.Numerics;

namespace Rin.Framework.Views.Graphics.Vector;

public class VectorPath : IPath
{
    public enum SegmentType
    {
        Line,
        Bezier
    }
    public struct Segment
    {
        public SegmentType Type;
        public Vector2 Begin;
        public Vector2 End;
        public Vector2 ControlA;
        public Vector2 ControlB;
    }
    
    
    public VectorPath(CommandList list, in Matrix4x4 transform, in Color? color = null)
    {
        _list = list;
        Transform = transform;
        Color = color ?? Color.White;
    }

    public List<Segment> Segments { get; } = [];
    public bool IsClosed { get; private set; }
    public bool IsFilled { get; private set; }
    public float StrokeWidth  { get; private set; }
    public Matrix4x4 Transform { get; private set; }

    public Vector2 Position { get; private set; }
    public Color Color { get; private set; }

    private readonly CommandList _list;

    public IPath Close()
    {
        IsClosed = true;
        return this;
    }

    public IPath MoveTo(in Vector2 end)
    {
        Position = end;
        return this;
    }

    public IPath LineTo(in Vector2 end)
    {
        Segments.Add(new Segment { Type = SegmentType.Line, Begin = end, End = end });
        return this;
    }

    public IPath BezierTo(in Vector2 controlA, in Vector2 controlB, in Vector2 end)
    {
        Segments.Add(new Segment { Type = SegmentType.Line, Begin = end, End = end,ControlA = controlA, ControlB = controlB });
        return this;
    }

    public CommandList Stroke(float width = 1)
    {
        StrokeWidth = width;
        IsFilled = false;
        return _list;
    }

    public CommandList Fill()
    {
        IsFilled = true;
        IsClosed = true;
        return _list;
    }
}