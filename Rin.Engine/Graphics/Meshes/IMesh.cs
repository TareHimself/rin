namespace Rin.Engine.Graphics.Meshes;

public interface IMesh
{
    public MeshSurface[] GetSurfaces();

    public MeshSurface GetSurface(int surfaceIndex);
    public IDeviceBufferView GetVertices();
    public IDeviceBufferView GetVertices(int surfaceIndex);
    public IDeviceBufferView GetIndices();

    public Bounds3D GetBounds();
}