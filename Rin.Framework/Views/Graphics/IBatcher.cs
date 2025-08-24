using Rin.Framework.Graphics;

namespace Rin.Framework.Views.Graphics;

public interface IBatcher
{
    void Draw(ViewsFrame frame, IBatch batch, in DeviceBufferView buffer);
    IBatch NewBatch();
}