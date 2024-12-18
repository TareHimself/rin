using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using rin.Framework.Graphics.Shaders.Rsl;

namespace rin.Framework.Views.Graphics;

public abstract class SimpleQuadBatcher<T>  : IBatcher where T : IBatch
{
    public void Draw(WidgetFrame frame, IBatch batch, IDeviceBuffer view)
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

    protected abstract IGraphicsShader GetShader();
    protected abstract T MakeNewBatch();

    /// <summary>
    /// Bind sets, write data, push constants and return the number of quads to draw
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="view"></param>
    /// <param name="batch"></param>
    /// <param name="shader"></param>
    /// <returns></returns>
    protected abstract uint WriteBatch(WidgetFrame frame, IDeviceBuffer view, T batch, IGraphicsShader shader);
}