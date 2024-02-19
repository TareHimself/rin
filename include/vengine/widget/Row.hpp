#pragma once
#include "Scrollable.hpp"
#include "SlotBase.hpp"

namespace vengine::widget {
class Row : public Scrollable<SlotBase> {
  std::optional<glm::dvec2> _lastMousePosition;
public:
  float scrollScale = 3.0f;
  
  std::optional<uint32_t> GetMaxSlots() const override;
  
  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  
  Size2D ComputeDesiredSize() const override;

  float GetMaxScroll() const override;
  bool IsScrollable() const override;
  
  bool OnScroll(const std::shared_ptr<window::ScrollEvent> &event) override;

  bool OnMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event) override;
  void OnMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event) override;
  bool OnMouseMoved(const std::shared_ptr<window::MouseMovedEvent> &event) override;
};
}
