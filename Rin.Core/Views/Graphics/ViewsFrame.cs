using System.Numerics;
using Rin.Core.Graphics;

namespace Rin.Core.Views.Graphics;

public class ViewsFrame(
    SurfaceContext context,
    IExecutionContext executionContext)
{
    public IExecutionContext ExecutionContext = executionContext;
    public Extent2D Extent = context.Extent;

    public Matrix4x4 ProjectionMatrix = context.ProjectionMatrix;
}