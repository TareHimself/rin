#include "vengine/widget/utils.hpp"

#include "vengine/widget/WidgetRoot.hpp"

namespace vengine::widget {
void bindMaterial(const widget::WidgetFrameData *frame,
                  Managed<drawing::MaterialInstance>& material) {
  const auto rawFrame = frame->GetRaw();
  material->SetBuffer("UiGlobalBuffer",frame->GetRoot()->GetGlobalBuffer());
  material->BindPipeline(rawFrame);
  material->BindSets(rawFrame);
}
}
