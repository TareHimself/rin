﻿using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Engine.Views.Graphics;

public abstract class SimpleQuadBatcher<T> : IBatcher where T : IBatch
{
    public void Draw(ViewsFrame frame, IBatch batch, in DeviceBufferView buffer)
    {
        var shader = GetShader();
        if (shader.Bind(frame.ExecutionContext))
        {
            var numQuads = WriteBatch(frame, buffer, (T)batch, GetShader());
            if (numQuads == 0) return;
            frame.ExecutionContext.Draw(6, numQuads);
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
    protected abstract uint WriteBatch(ViewsFrame frame, in DeviceBufferView view, T batch, IGraphicsShader shader);
}