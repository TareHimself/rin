using rin.Graphics;
using rin.Graphics.Shaders;

namespace rin.Widgets.Graphics;

public abstract class SimpleQuadBatcher<T>  : IBatcher where T : IBatch
{
    public void Draw(WidgetFrame frame, IBatch batch, DeviceBuffer.View view)
    {
        var shader = GetShader();
        var cmd = frame.Raw.GetCommandBuffer();
        frame.BeginMainPass();
        if (shader.Bind(cmd))
        {
            var numQuads = WriteBatch(frame,view,(T)batch,GetShader());
            cmd.Draw(6,numQuads);
        }
    }

    public IBatch NewBatch()
    {
        return MakeNewBatch();
    }

    protected abstract GraphicsShader GetShader();
    protected abstract T MakeNewBatch();
    
    /// <summary>
    /// Bind sets, write data, push constants and return the number of quads to draw
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="view"></param>
    /// <param name="batch"></param>
    /// <param name="shader"></param>
    /// <returns></returns>
    protected abstract uint WriteBatch(WidgetFrame frame, DeviceBuffer.View view,T batch,GraphicsShader shader);
}