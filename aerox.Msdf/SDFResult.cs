namespace aerox.Msdf;

public class SDFResult
{
    public int Width = 0;
    public int Height = 0;
    public int Channels = 0;
    public ByteBuffer Data;

    public SDFResult(ByteBuffer data)
    {
        Data = data;
    }
}