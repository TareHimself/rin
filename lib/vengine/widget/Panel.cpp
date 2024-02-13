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
  const auto myDrawRect = CalculateFinalRect(info.drawRect);
  for(auto &slot : _slots.clone()) {
    const DrawInfo childInfo{this,ComputeSlotRect(slot).OffsetBy(myDrawRect)};
    slot->GetWidget().Reserve()->Draw(frameData,childInfo);
  }
}

Rect Panel::ComputeSlotRect(const Managed<PanelSlot> &child) {
  Rect result;

  const auto size = this->GetDesiredSize();
  auto slotRect = child->GetRect();
  
  if(child->GetSizeToContent()) {
    const auto childSize = child->GetWidget().Reserve()->GetDesiredSize();
    slotRect.width = childSize.width;
    slotRect.height = childSize.height;
  }
  
  const auto slotWidth =  size.width;
  const auto slotHeight = size.height;
  
  auto x1 = slotRect.x;
  auto x2 = slotRect.x + slotRect.width;
  auto y1 = slotRect.y;
  auto y2 = slotRect.y + slotRect.height;

  const auto anchorX = child->GetAnchorX();
  const auto anchorY = child->GetAnchorY();
  
  x1 += slotWidth * anchorX.min;
  x2 += slotWidth * anchorX.max;
  y1 += slotHeight * anchorY.min;
  y2 += slotHeight * anchorY.max;

  result.x = x1;
  result.y = y1;
  result.width = x2 - x1;
  result.height = y2 - y1;
  return result;
}

std::optional<uint32_t> Panel::GetMaxSlots() const {
  return {};
}


}
