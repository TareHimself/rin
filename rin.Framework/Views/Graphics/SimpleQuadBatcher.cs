using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Views.Graphics;

public abstract class SimpleQuadBatcher<T> : IBatcher where T : IBatch
{
    public void Draw(ViewsFrame frame, IBatch batch, IDeviceBufferView buffer)
    {
        var shader = GetShader();
        var cmd = frame.Raw.GetCommandBuffer();
        frame.BeginMainPass();
        if (shader.Bind(cmd))
        {
            var numQuads = WriteBatch(frame, buffer, (T)batch, GetShader());
            if (numQuads == 0) return;
            cmd.Draw(6, numQuads);
        }
    }

    public IBatch NewBatch()
    {
        return MakeNewBatch();
    }

    protected abstract IGraphicsShader GetShader();
    protected abstract T MakeNewBatch();

    /// <summary>
    ///     Bind sets, write data, push constants and return the number of quads to draw
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="view"></param>
    /// <param name="batch"></param>
    /// <param name="shader"></param>
    /// <returns></returns>
    protected abstract uint WriteBatch(ViewsFrame frame, IDeviceBufferView view, T batch, IGraphicsShader shader);
}