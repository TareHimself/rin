using rin.Framework.Graphics;

namespace rin.Framework.Views.Graphics;

public interface IBatcher
{
    abstract void Draw(ViewsFrame frame, IBatch batch, IDeviceBuffer buffer);
    abstract IBatch NewBatch();
}