#include <vengine/widget/types.hpp>

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

Point2D::Point2D() {
  x = 0;
  y = 0;
}

// Point2D::Point2D(const std::variant<float, int, double> &inX,
//     const std::variant<float, int, double> &inY) {
//   x = std::get<float>(inX);
//   y = std::get<float>(inY);
// }

Point2D::Point2D(const float inX, const float inY) {
  x = inX;
  y = inY;
}

Point2D::Point2D(const double inX, const double inY) {
  x = static_cast<float>(inX);
  y = static_cast<float>(inY);
}

Point2D::Point2D(const int inX, const int inY) {
  x = static_cast<float>(inX);
  y = static_cast<float>(inY);
}

Size2D::Size2D() {
  width = 0;
  height = 0;
}

Size2D::Size2D(const float inWidth, const float inHeight) {
  width = inWidth;
  height = inHeight;
}

Size2D::Size2D(const vk::Extent2D &extent) {
  width = static_cast<float>(extent.width);
  height = static_cast<float>(extent.height);
}

bool Size2D::operator==(const Size2D & other) const {
  return width == other.width && height == other.height;
}

Rect::operator glm::vec<4, float, glm::packed_highp>() const {
  return glm::vec4{x,y,width,height};
}

Rect Rect::ApplyPivot(const Point2D &pivot) const {
  Rect result = *this;
  result.x -= result.width * pivot.x;
  result.y -= result.height * pivot.y;
  return result;
}

Rect Rect::OffsetBy(const Rect &other) const {
  Rect result = *this;
  result.x += other.x;
  result.y += other.y;

  return result;
}
}
