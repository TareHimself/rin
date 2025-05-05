using System.Numerics;
using Rin.Engine.Views.Graphics.Commands;

namespace rin.Examples.ViewsTest;

public class CustomShaderCommand(Matrix4x4 transform, Vector2 size, bool hovered, Vector2 cursorPosition)
    : TCommand<CustomShaderPass>
{
    public Matrix4x4 Transform => transform;
    public Vector2 Size => size;
    public bool Hovered => hovered;

    public Vector2 CursorPosition => cursorPosition;
}