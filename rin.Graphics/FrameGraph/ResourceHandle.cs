namespace rin.Graphics.FrameGraph;

public struct ResourceHandle(string id) : IResourceHandle
{
    private readonly string _id = id;

    public bool Equals(IResourceHandle? other)
    {
        return other is ResourceHandle asResourceHandle && asResourceHandle._id == _id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ResourceHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }
}