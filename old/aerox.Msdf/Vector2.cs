namespace aerox.Msdf;

/// <summary>
/// Simple 2 Component Vector Class
/// </summary>
/// <param name="x"></param>
/// <param name="y"></param>
public struct Vector2(float x, float y)
{
    public float X = x;
    public float Y = y;

    public Vector2() : this(0)
    {
        
    }

    public Vector2(float data) : this(data, data)
    {
        
    }
}