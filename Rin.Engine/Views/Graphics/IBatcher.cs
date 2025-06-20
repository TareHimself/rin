using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Graphics;

public interface IBatcher
{
    void Draw(ViewsFrame frame, IBatch batch, in DeviceBufferView buffer);
    IBatch NewBatch();
}