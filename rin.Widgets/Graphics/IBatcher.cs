using rin.Graphics;

namespace rin.Widgets.Graphics;

public interface IBatcher
{
    abstract void Draw(WidgetFrame frame, IBatch batch,DeviceBuffer.View view);
    abstract IBatch NewBatch();
}