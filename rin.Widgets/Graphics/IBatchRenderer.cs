using rin.Graphics;

namespace rin.Widgets.Graphics;

public interface IBatchRenderer
{
    abstract void Draw(WidgetFrame frame, IBatch batch, DeviceBuffer buffer, ulong address, ulong offset);
    abstract IBatch NewBatch();
}