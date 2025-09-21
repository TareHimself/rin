using System.Numerics;

namespace Rin.Framework.Views.Graphics.Vector;

public interface IPath
{
    public Vector2 Position { get; }
    public IPath Close();
    public IPath MoveTo(in Vector2 end);
    public IPath LineTo(in Vector2 end);
    public IPath BezierTo(in Vector2 controlA, in Vector2 controlB, in Vector2 end);
    public CommandList Stroke(float width = 1.0f);
    public CommandList Fill();
}