#pragma once
#include "vengine/drawing/MaterialInstance.hpp"
#include "vengine/widget/types.hpp"

namespace vengine::widget {
void bindMaterial(const widget::WidgetFrameData * frame,Managed<drawing::MaterialInstance>& material);
}
