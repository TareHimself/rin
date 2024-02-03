#include <vengine/widget/Widget.hpp>
#include <vengine/widget/WidgetManager.hpp>
#include "vengine/Engine.hpp"
#include "vengine/math/utils.hpp"

namespace vengine::widget {
void Widget::Init(WidgetManager * outer) {
  Object<WidgetManager>::Init(outer);
  _createdAt = GetOuter()->GetOuter()->GetEngineTimeSeconds();
}

void Widget::SetParent(Widget *ptr) {
  _parent = ptr;
}

Widget * Widget::GetParent() const {
  return _parent;
}

Array<WeakRef<Widget>> Widget::GetChildren() const {
  Array<WeakRef<Widget>> result;
  for(auto &child : _children) {
    result.Push(child);
  }
  
  return result;
}

void Widget::SetRect(const vk::Rect2D rect) {
  _rect = rect;
}

vk::Rect2D Widget::GetRect() const {
  return _rect;
}

void Widget::Draw(drawing::Drawer * drawer, drawing::SimpleFrameData *frameData,
                  WidgetParentInfo parentInfo) {
  DrawSelf(drawer,frameData,parentInfo);
  
  if(_children.empty()) {
    WidgetParentInfo myInfo;
    myInfo.rect = GetRect();
    myInfo.widget = this;
    for(const auto child : _children) {
      child->Draw(drawer,frameData,parentInfo);
    }
  }
  
}

void Widget::DrawSelf(drawing::Drawer * drawer, drawing::SimpleFrameData *frameData,
    WidgetParentInfo parentInfo) {
  const auto manager = GetOuter();
  const auto engine = manager->GetEngine();
  const auto material = manager->GetDefaultMaterial().Reserve();
  const auto ogFrameData = frameData->GetRaw();
  const auto screenSize = engine->GetWindowExtent();
  WidgetPushConstants drawData{};
  const auto curTime =  engine->GetEngineTimeSeconds() - _createdAt;
  const auto sinModified = (sin(curTime + cos(_createdAt))  + 1) / 2;
  const auto sinModified2 = (sin(curTime + sinModified * 2)  + 1) / 2;
  uint32_t width = math::mapRange(sinModified2, 0, 1, 256, 512);
  uint32_t height = math::mapRange(sinModified, 0, 1, 256, 512);
  SetRect({{static_cast<int32_t>(sinModified * (screenSize.width - width)),static_cast<int32_t>(sinModified2 * (screenSize.height - height))},{width,height}});

  const auto rect = GetRect();
  drawData.extent = glm::vec4{rect.offset.x,rect.offset.y,rect.extent.width,rect.extent.height};
  drawData.time.x = curTime;
  
  material->BindPipeline(ogFrameData);
  material->BindSets(ogFrameData);
  //material->BindCustomSet(ogFrameData,frameData->GetDescriptor(),0);
  material->PushConstant(frameData->GetCmd(),"pRect",drawData);
  
  frameData->GetCmd()->draw(6,1,0,0);
}

void Widget::HandleDestroy() {
  Object<WidgetManager>::HandleDestroy();
  _children.clear();
}
}
