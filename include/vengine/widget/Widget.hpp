#pragma once
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"

namespace vengine {
namespace drawing {
struct RawFrameData;
class Drawer;
}
}

namespace vengine::widget {
class WidgetManager;

class Widget : public Object<WidgetManager> {
  Widget * _parent = nullptr;
  Array<Pointer<Widget>> _children;
  vk::Rect2D _rect;
  float _createdAt = 0.0f;
public:
  void Init(WidgetManager * outer) override;
  
  // virtual void Init(Widget * parent);

  void SetParent(Widget * ptr);
  
  Widget * GetParent() const;
  
  Array<WeakPointer<Widget>> GetChildren() const;

  virtual void SetRect(vk::Rect2D rect);
  virtual vk::Rect2D GetRect() const;
  
  virtual void Draw(drawing::Drawer * drawer, drawing::SimpleFrameData *
                    frameData, WidgetParentInfo
                    parentInfo);

  virtual void DrawSelf(drawing::Drawer * drawer, drawing::SimpleFrameData *
                        frameData, WidgetParentInfo
                        parentInfo);

  void HandleDestroy() override;
};
}

