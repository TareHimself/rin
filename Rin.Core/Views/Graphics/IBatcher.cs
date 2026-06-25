using Rin.Core.Graphics;

namespace Rin.Core.Views.Graphics;

public interface IBatcher
{
    void Draw(ViewsFrame frame, IBatch batch, in DeviceBufferView buffer);
    IBatch NewBatch();
}