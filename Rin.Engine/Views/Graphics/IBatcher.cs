using Rin.Engine.Graphics;

namespace Rin.Engine.Views.Graphics;

public interface IBatcher
{
    void Draw(ViewsFrame frame, IBatch batch, IDeviceBufferView buffer);
    IBatch NewBatch();
}