#include "aerox/widgets/utils.hpp"

#include "aerox/widgets/WidgetRoot.hpp"

namespace aerox::widgets {
void bindMaterial(const widgets::WidgetFrameData *frame,
                  std::shared_ptr<drawing::MaterialInstance>& material) {
  const auto rawFrame = frame->GetRaw();
  material->SetBuffer("UiGlobalBuffer",frame->GetRoot()->GetGlobalBuffer().lock());
  material->BindPipeline(rawFrame);
  material->BindSets(rawFrame);
}
}
