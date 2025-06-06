using System.Numerics;
using Rin.Engine.Graphics;

namespace Rin.Engine.World.Graphics;

public class WorldFrame(
    Matrix4x4 view,
    Matrix4x4 projection,
    IDeviceBufferView worldInfo,
    IExecutionContext executionContext)
{
    public Matrix4x4 View => view;
    public Matrix4x4 Projection => projection;

    public Matrix4x4 ViewProjection { get; } = projection * view;

    public IDeviceBufferView SceneInfo => worldInfo;

    public IExecutionContext ExecutionContext { get; } = executionContext;
}