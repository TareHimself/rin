﻿using rin.Framework.Graphics;

namespace rin.Framework.Views.Graphics;

public interface IBatcher
{
    abstract void Draw(WidgetFrame frame, IBatch batch, IDeviceBuffer view);
    abstract IBatch NewBatch();
}