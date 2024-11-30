using rin.Graphics;

namespace rin.Widgets.Graphics;

public interface IBatcher
{
    abstract void Draw(WidgetFrame frame, IBatch batch, IDeviceBuffer view);
    abstract IBatch NewBatch();
}