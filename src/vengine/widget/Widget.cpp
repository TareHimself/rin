#include "Widget.hpp"
#include "WidgetManager.hpp"

namespace vengine::widget {
void Widget::Init(WidgetManager *outer) {
  Object<WidgetManager>::Init(outer);
}

Widget * Widget::GetParent() const {
  return _parent;
}

Array<Widget *> Widget::GetChildren() const {
  return _children;
}

vk::Extent2D Widget::GetSize() const {
  return _desiredSize;
}

void Widget::Draw(drawing::Drawer *drawer, drawing::SimpleFrameData *frameData,
                  WidgetParentInfo parentInfo) {
  DrawSelf(drawer,frameData,parentInfo);
  
  if(_children.empty()) {
    WidgetParentInfo myInfo;
    myInfo.extent = GetSize();
    myInfo.widget = this;
    for(const auto child : _children) {
      child->Draw(drawer,frameData,parentInfo);
    }
  }
  
}

void Widget::DrawSelf(drawing::Drawer *drawer, drawing::SimpleFrameData *frameData,
    WidgetParentInfo parentInfo) {
  const auto material = GetOuter()->GetDefaultRectMaterial();
  const auto ogFrameData = frameData->GetRaw();
  
  drawing::WidgetPushConstants drawData{};
  drawData.extent = glm::vec4{0,0,100,100};
  
  material->BindPipeline(ogFrameData);
  material->BindSets(ogFrameData);
  //material->BindCustomSet(ogFrameData,frameData->GetDescriptor(),0);
  material->PushConstant(frameData->GetCmd(),"pRect",drawData);
  
  frameData->GetCmd()->draw(6,1,0,0);
}

void Widget::HandleDestroy() {
  Object<WidgetManager>::HandleDestroy();
  for(const auto widget : _children) {
    widget->Destroy();
  }

  _children.clear();
}
}
