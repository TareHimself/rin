#include "vengine/widget/Panel.hpp"

namespace vengine::widget {
PanelSlot::PanelSlot(const Managed<Widget> &widget)
  : SlotBase(widget), _sizeToContent(false) {
  _rect = {};
}

void PanelSlot::SetMinAnchor(const Point2D &anchor) {
  _minAnchor = anchor;
}

void PanelSlot::SetMaxAnchor(const Point2D &anchor) {
  _maxAnchor = anchor;
}

void PanelSlot::SetAlignment(const Point2D &alignment) {
  _alignment = alignment;
}

void PanelSlot::SetRect(const Rect &rect) {
  _rect = rect;
}

void PanelSlot::SetSizeToContent(const bool val) {
  _sizeToContent = val;
}

Point2D PanelSlot::GetMinAnchor() const {
  return _minAnchor;
}

Point2D PanelSlot::GetMaxAnchor() const {
  return _maxAnchor;
}

Point2D PanelSlot::GetAlignment() const {
  return _alignment;
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

void Panel::Draw(WidgetFrameData *frameData,
                 const DrawInfo info) {

  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  //const auto myDrawRect = CalculateFinalRect(info.drawRect);
  for(auto &slot : _slots.clone()) {
    const DrawInfo childInfo{this,clip};
    auto slotRect = ComputeSlotRect(slot).Offset(GetDrawRect().GetPoint());
    slot->GetWidget().Reserve()->UpdateDrawRect(slotRect);
    
    slot->GetWidget().Reserve()->Draw(frameData,childInfo);
  }
}

Rect Panel::ComputeSlotRect(const Managed<PanelSlot> &child) const {
  const auto size = this->GetDrawRect().GetSize();
  auto slotRect = child->GetRect();
  
  

  const auto anchorMin = child->GetMinAnchor();
  const auto anchorMax = child->GetMaxAnchor();

  const auto noOffsetX = utils::nearlyEqual(anchorMin.x,anchorMax.x,0.001f);
  const auto noOffsetY = utils::nearlyEqual(anchorMin.y,anchorMax.y,0.001f);
  
  if(child->GetSizeToContent()) {
    auto childSize = child->GetWidget().Reserve()->GetDesiredSize();
    if(!noOffsetX) {
      childSize.width = slotRect.GetSize().width;
    }

    if(!noOffsetY) {
      childSize.height = slotRect.GetSize().height;
    }
    
    slotRect.SetSize(childSize);
  }

  auto p1 = slotRect.GetPoint();
  auto p2 = p1 + slotRect.GetSize();
  
  if(noOffsetX) {
    const auto xPosition = size.width * anchorMin.x;
    p1.x += xPosition;
    p2.x += xPosition;
  } else {
    p1.x = (size.width * anchorMin.x) + p1.x;
    p2.x = (size.width * anchorMax.x) - p2.x;
  }

  if(noOffsetY) {
    const auto yPosition = size.height * anchorMin.y;
    p1.y += yPosition;
    p2.y += yPosition;
  } else {
    p1.y = (size.height * anchorMin.y) + p1.y;
    p2.y = (size.height * anchorMax.y) - p2.y;
  }
  
  // p1 = p1 + Point2D{slotWidth * anchorX.min,slotHeight * anchorY.min};
  // p2 = p2 + Point2D{slotWidth * anchorX.max,slotHeight * anchorY.max};
  auto dist = p2 - p1;
  const auto alignment = child->GetAlignment();
  
  dist = dist * alignment;

  p1 = p1 - dist;
  p2 = p2 - dist;
  
  return {p1,p2};
}

std::optional<uint32_t> Panel::GetMaxSlots() const {
  return {};
}


}
