using rin.Framework.Graphics;

namespace rin.Framework.Views.Graphics;

public interface IBatcher
{
    void Draw(ViewsFrame frame, IBatch batch, IDeviceBufferView buffer);
    IBatch NewBatch();
}