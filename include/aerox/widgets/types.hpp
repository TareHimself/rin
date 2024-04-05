#pragma once
#include "aerox/drawing/types.hpp"
#include <glm/matrix.hpp>
#include <glm/fwd.hpp>
#include <vulkan/vulkan.hpp>

namespace aerox {
namespace widgets {
class WidgetRoot;
}
}

namespace aerox::widgets {
class Widget;
class Size2D;

struct Point2D {
  float x;
  float y;
  Point2D();
  Point2D(float inX,float inY);
  Point2D(double inX,double inY);
  Point2D(int inX,int inY);

  Point2D operator+(const Size2D& other) const;
  
  Point2D operator+(const Point2D& other) const;
  Point2D operator-(const Point2D& other) const;
  Point2D operator/(const Point2D& other) const;
  Point2D operator*(const Point2D& other) const;

  Point2D operator+(const float& other) const;
  Point2D operator-(const float& other) const;
  Point2D operator/(const float& other) const;
  Point2D operator*(const float& other) const;

  bool operator==(const Point2D &) const;
};

struct Size2D {
  float width;
  float height;

  Size2D();
  Size2D(float inWidth,float inHeight);
  Size2D(const vk::Extent2D& extent);

  bool operator==(const Size2D &) const;

  Size2D& operator=(const Point2D& other);
  Size2D& operator=(const glm::ivec2& other);
};

struct Rect {
private:
  Point2D _point;
  Size2D _size;

public:
  Rect();
  Rect(const Point2D& p1,const Point2D& p2);
  Point2D GetPoint() const;
  Size2D GetSize() const;
  
  Rect& SetSize(const Size2D& size);
  Rect& SetPoint(const Point2D& point);
  operator glm::vec4() const;

  Rect& Pivot(const Point2D& pivot);

  Rect& Offset(const Point2D& offset);

  Rect& Clamp(const Rect& area);

  Rect Clone() const;

  bool IsWithin(const Point2D& point) const;

  bool HasIntersection(const Rect& rect) const;

  bool HasSpace() const;

  bool operator==(const Rect &) const;
};

struct DrawInfo {
  Widget * parent = nullptr;
  // The current clipping area
  Rect clip;

  // // The area you have been given to draw at
  // Rect drawRect;
};

struct WidgetFrameData : drawing::SimpleFrameData {
private:
  WidgetRoot * _root = nullptr;
public:
  WidgetFrameData(drawing::RawFrameData * frame,WidgetRoot * root);
  
  WidgetRoot * GetRoot() const;
};

struct UiGlobalBuffer {
  glm::vec4 viewport{0};
  glm::vec4 time{0};
};

struct WidgetPushConstants {
  
  glm::vec4 clip{0};
  glm::vec4 extent{0};
  glm::mat4 transform{1.0f};
};

enum EVisibility {
  Visibility_Visible,
  Visibility_Hidden,
  Visibility_VisibleNoHitTest,
  Visibility_Collapsed
};

}


