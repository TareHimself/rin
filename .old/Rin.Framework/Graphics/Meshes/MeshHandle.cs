namespace Rin.Framework.Graphics.Meshes;

public readonly record struct MeshHandle
{
    private readonly int _data;
    
    public MeshHandle(int data)
    {
        _data = data;
    }
    
    public static MeshHandle InvalidMesh => new(-1);

    // public bool IsValid()
    // {
    //     return Id > 0 && IGraphicsModule.Get().GetBindlessImageFactory().IsValid(this);
    // }

    public static explicit operator int(MeshHandle handle)
    {
        return handle._data;
    }
}