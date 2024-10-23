using aerox.Runtime.Graphics;
using aerox.Runtime.Widgets.Graphics;

namespace aerox.Runtime.Widgets;

public interface IBatchRenderer
{
    abstract void Draw(WidgetFrame frame,IBatch batch, DeviceBuffer buffer, ulong offset);
}