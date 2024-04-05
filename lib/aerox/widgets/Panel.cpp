#include "aerox/widgets/Panel.hpp"

namespace aerox::widgets {
PanelSlot::PanelSlot(const std::shared_ptr<Widget> &widget)
  : SlotBase(widget), _sizeToContent(false) {
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

void PanelSlot::SetPoint(const Point2D &point) {
  _point = point;
}


void PanelSlot::SetSize(const panelSlotSize &size) {
  _size = size;
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

Point2D PanelSlot::GetPoint() const {
  return _point;
}

std::pair<std::shared_ptr<Unit>, std::shared_ptr<Unit>> PanelSlot::GetSize() const {
  return {_size.first,_size.second};
}

bool PanelSlot::GetSizeToContent() const {
  return _sizeToContent;
}

void Panel::Draw(WidgetFrameData *frameData,
                 const DrawInfo info) {

  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }

  const auto clip = info.clip.Clone().Clamp(GetDrawRect());
  //const auto myDrawRect = CalculateFinalRect(info.drawRect);
  for(auto &slot : _slots.clone()) {
    if(const auto widget = slot->GetWidget().lock()) {
      const DrawInfo childInfo{this,clip};
      
      auto slotRect = ComputeSlotRect(slot).Offset(GetDrawRect().GetPoint());
      
      widget->UpdateDrawRect(slotRect);
    
      widget->Draw(frameData,childInfo);
    }
    
  }
}

Rect Panel::ComputeSlotRect(const std::shared_ptr<PanelSlot> &child) const {
  // if(child->computed.has_value()) {
  //   return child->computed.value();
  // }
  
  const auto size = this->GetDrawRect().GetSize();

  auto [fst, snd] = child->GetSize();
  
  Rect slotRect = {child->GetPoint(),{fst->Compute(size.width),snd->Compute(size.height)}};
  
  const auto anchorMin = child->GetMinAnchor();
  const auto anchorMax = child->GetMaxAnchor();

  const auto noOffsetX = utils::nearlyEqual(anchorMin.x,anchorMax.x,0.001f);
  const auto noOffsetY = utils::nearlyEqual(anchorMin.y,anchorMax.y,0.001f);
  
  if(const auto widget = child->GetWidget().lock(); child->GetSizeToContent()) {
    auto childSize = widget->GetDesiredSize();
    
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

  child->computed = {p1,p2};
  return child->computed.value();
}

std::optional<uint32_t> Panel::GetMaxSlots() const {
  return {};
}

}
