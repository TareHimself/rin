using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace Examples.ImageStrip;

public class CustomShaderCommand(Matrix4x4 transform, Vector2 size, Vector2 focus, BorderRadius borderRadius)
    : TCommand<MainPassConfig, CustomShaderCommandHandler>
{
    public Matrix4x4 Transform => transform;
    public Vector2 Size => size;

    /// <summary>
    ///     The point the pattern is centered on, in the view's own space.
    /// </summary>
    public Vector2 Focus => focus;

    public BorderRadius BorderRadius => borderRadius;
}