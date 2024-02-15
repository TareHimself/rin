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

Point2D Point2D::operator+(const Size2D &other) const {
  return {x + other.width,y + other.height};
}

Point2D Point2D::operator+(const Point2D &other) const {
  return {x + other.x,y + other.y};
}

Point2D Point2D::operator-(const Point2D &other) const {
  return {x - other.x,y - other.y};
}

Point2D Point2D::operator/(const Point2D &other) const {
  return {x / other.x,y / other.y};
}

Point2D Point2D::operator*(const Point2D &other) const {
  return {x * other.x,y * other.y};
}

Point2D Point2D::operator+(const float &other) const {
  return *this + Point2D{other,other};
}

Point2D Point2D::operator-(const float &other) const {
  return *this - Point2D{other,other};
}

Point2D Point2D::operator/(const float &other) const {
  return *this / Point2D{other,other};
}

Point2D Point2D::operator*(const float &other) const {
  return *this * Point2D{other,other};
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

Size2D & Size2D::operator=(const Point2D &other) {
  width = other.x;
  height = other.y;
  return *this;
}

Rect::Rect() = default;

Rect::Rect(const Point2D &p1, const Point2D &p2) {
  _point = p1;
  _size = p2 - p1;
}

Point2D Rect::GetPoint() const {
  return _point;
}

Size2D Rect::GetSize() const {
  return _size;
}

Rect & Rect::SetSize(const Size2D &size) {
  _size = size;
  return *this;
}

Rect & Rect::SetPoint(const Point2D &point) {
  _point = point;
  return *this;
}

Rect::operator glm::vec<4, float, glm::packed_highp>() const {
  return glm::vec4{_point.x,_point.y,_size.width,_size.height};
}

Rect& Rect::Pivot(const Point2D &pivot) {
  _point.x -= _size.width * pivot.x;
  _point.y -= _size.height * pivot.y;
  return *this;
}

Rect& Rect::Offset(const Point2D &offset){
  _point.x += offset.x;
  _point.y += offset.y;
  return *this;
}

Rect& Rect::Clamp(const Rect &area){

  if(!HasIntersection(area)) {
    return SetSize({0,0}).SetPoint(area.GetPoint());
  }
  
  const auto a1 = GetPoint();
  const auto a2 = a1 + GetSize();
  const auto b1 = area.GetPoint();
  const auto b2 = b1 + area.GetSize();
  
  _point = {std::max(a1.x,b1.x),std::max(b1.y,b1.y)};
  auto p2 = Point2D{std::min(a2.x,b2.x),std::min(a2.y,b2.y)};
  _size = p2 - _point;
  
  return *this;
}

Rect Rect::Clone() const {
  return *this;
}

bool Rect::IsWithin(const Point2D &point) const {
  const auto isWithinHorizontal = _point.x <= point.x && point.x <= _point.x + _size.width;
  const auto isWithinVertical = _point.y <= point.y && point.y <= _point.y + _size.height;
  return isWithinHorizontal && isWithinVertical;
}

bool Rect::HasIntersection(const Rect &rect) const {
  const auto a1 = GetPoint();
  const auto a2 = a1 + GetSize();
  const auto b1 = rect.GetPoint();
  const auto b2 = b1 + rect.GetSize();
  
  if(a1.x <= b1.x) {
    if(a1.y <= b1.y) {
      return b1.x <= a2.x && b1.y <= a2.y; // A top left B bottom right
    } else {
      return b1.x <= a2.x && a1.y <= b2.y; // A Bottom left B Top right
    }
  } else {
    if(a1.y <= b1.y) {
      return a1.x <= b2.x && b1.y <= a2.y; // A top right B bottom left
    } else {
      return a1.x <= b2.x && a1.y <= b2.y; // A bottom right B top left
    }
  }
}

bool Rect::HasSpace() const {
  return GetSize().height != 0 && GetSize().width != 0;
}

}
