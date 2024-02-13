#pragma once
#include "vengine/drawing/types.hpp"
#include <vulkan/vulkan.hpp>

namespace vengine::widget {
class Widget;

struct Point2D {
  float x;
  float y;
  Point2D();
  Point2D(float inX,float inY);
  Point2D(double inX,double inY);
  Point2D(int inX,int inY);
};

struct Size2D {
  float width;
  float height;

  Size2D();
  Size2D(float inWidth,float inHeight);
  Size2D(const vk::Extent2D& extent);

  bool operator==(const Size2D &) const;
};

struct Rect : Point2D, Size2D{


  operator glm::vec4() const;

  Rect ApplyPivot(const Point2D& pivot) const;

  Rect OffsetBy(const Rect& other) const;
};

struct DrawInfo {
  Widget * parent = nullptr;
  Rect drawRect;
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

struct UiGlobalBuffer {
  glm::vec4 viewport{0};
};

struct WidgetPushConstants {
  glm::vec4 extent{0};
  glm::vec4 time{0};
};

enum EVisibility {
  Visibility_Visible,
  Visibility_Hidden,
  Visibility_VisibleNoHitTest,
  Visibility_Collapsed
};

}


