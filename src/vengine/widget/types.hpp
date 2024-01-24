#pragma once
#include "vengine/drawing/types.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine::widget {
class Widget;

struct WidgetParentInfo {
  Widget * widget = nullptr;
  vk::Extent2D extent;
};

struct WidgetFrameData {
private:
  drawing::RawFrameData * _frame = nullptr;
  vk::DescriptorSet _widgetDescriptor;
public:
  WidgetFrameData(drawing::RawFrameData * frame);

  vk::CommandBuffer * GetCmd() const;

  vk::DescriptorSet GetWidgetDescriptor() const;
  
  void SetWidgetDescriptor(const vk::DescriptorSet &descriptor);

  CleanupQueue * GetCleaner() const;

  drawing::RawFrameData * GetDrawerFrameData() const;
};
}


