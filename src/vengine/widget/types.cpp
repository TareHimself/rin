#include "types.hpp"

namespace vengine::widget {
WidgetFrameData::WidgetFrameData(drawing::RawFrameData *frame) {
  _frame  = frame;
}

vk::CommandBuffer * WidgetFrameData::GetCmd() const {
  return _frame->GetCmd();
}

vk::DescriptorSet WidgetFrameData::GetWidgetDescriptor() const{
  return _widgetDescriptor;
}

void WidgetFrameData::SetWidgetDescriptor(const vk::DescriptorSet &descriptor) {
  _widgetDescriptor = descriptor;
}

CleanupQueue * WidgetFrameData::GetCleaner() const {
  return &_frame->cleaner;
}

drawing::RawFrameData * WidgetFrameData::GetDrawerFrameData() const {
  return _frame;
}
}