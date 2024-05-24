using System.Runtime.InteropServices;

namespace aerox.Runtime.Graphics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct TextureHandle : IDisposable
{
    public int TextureId;
    
    // public Texture Resolve()
    // {
    //     var subsystem = Runtime.Instance.GetModule<GraphicsModule>();
    //     return 
    // }
    
    public void Dispose()
    {
        // Resolve().Dispose();
    }
}