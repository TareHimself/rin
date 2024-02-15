#include "vengine/widget/Panel.hpp"

namespace vengine::widget {
PanelSlot::PanelSlot(const Managed<Widget> &widget)
  : SlotBase(widget), _sizeToContent(false) {
  _xAnchor = {};
  _yAnchor = {};
  _rect = {};
}

void PanelSlot::SetAnchorX(const Anchor &anchor) {
  _xAnchor = anchor;
}

void PanelSlot::SetAnchorY(const Anchor &anchor) {
  _yAnchor = anchor;
}

void PanelSlot::SetRect(const Rect &rect) {
  _rect = rect;
}

void PanelSlot::SetSizeToContent(const bool val) {
  _sizeToContent = val;
}

Anchor PanelSlot::GetAnchorX() const {
  return _xAnchor;
}

Anchor PanelSlot::GetAnchorY() const {
  return _yAnchor;
}

Rect PanelSlot::GetRect() const {
  return _rect;
}

bool PanelSlot::GetSizeToContent() const {
  return _sizeToContent;
}

void Panel::Init(WidgetSubsystem *outer) {
  Widget::Init(outer);
}

void Panel::BeforeDestroy() {
  Widget::BeforeDestroy();
}

void Panel::Draw(drawing::SimpleFrameData *frameData,
                 const DrawInfo info) {

  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  //const auto myDrawRect = CalculateFinalRect(info.drawRect);
  for(auto &slot : _slots.clone()) {
    const DrawInfo childInfo{this,clip};
    slot->GetWidget().Reserve()->UpdateDrawRect(ComputeSlotRect(slot).Offset(GetDrawRect().GetPoint()));
    
    slot->GetWidget().Reserve()->Draw(frameData,childInfo);
  }
}

Rect Panel::ComputeSlotRect(const Managed<PanelSlot> &child) {
  const auto size = this->GetDesiredSize();
  auto slotRect = child->GetRect();
  
  if(child->GetSizeToContent()) {
    const auto childSize = child->GetWidget().Reserve()->GetDesiredSize();
    slotRect.SetSize(childSize);
  }
  
  const auto slotWidth =  size.width;
  const auto slotHeight = size.height;

  auto p1 = slotRect.GetPoint();
  auto p2 = p1 + slotRect.GetSize();

  const auto anchorX = child->GetAnchorX();
  const auto anchorY = child->GetAnchorY();

  p1 = p1 + Point2D{slotWidth * anchorX.min,slotHeight * anchorY.min};
  p2 = p2 + Point2D{slotWidth * anchorX.max,slotHeight * anchorY.max};
  
  return {p1,p2};
}

std::optional<uint32_t> Panel::GetMaxSlots() const {
  return {};
}


}
