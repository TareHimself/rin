#pragma once
#include "aerox/drawing/MaterialInstance.hpp"
#include "aerox/widgets/types.hpp"

namespace aerox::widgets {
void bindMaterial(const widgets::WidgetFrameData * frame,std::shared_ptr<drawing::MaterialInstance>& material);
}
