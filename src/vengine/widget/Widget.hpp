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
  Array<Widget *> _children;
  vk::Extent2D _desiredSize;
public:
  void Init(WidgetManager *outer) override;
  
  // virtual void Init(Widget * parent);
  
  Widget * GetParent() const;
  
  Array<Widget *> GetChildren() const;
  
  virtual vk::Extent2D GetSize() const;
  
  virtual void Draw(drawing::Drawer *drawer, drawing::SimpleFrameData *frameData, WidgetParentInfo
                    parentInfo);

  virtual void DrawSelf(drawing::Drawer *drawer, drawing::SimpleFrameData *frameData, WidgetParentInfo
                        parentInfo);

  void HandleDestroy() override;
};
}

