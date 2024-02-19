#pragma once
#include "ISlot.hpp"
#include "SlotBase.hpp"
#include "TMultiSlotWidget.hpp"
#include "Widget.hpp"

namespace vengine::widget {

struct Anchor {
  float min;
  float max;
};

class PanelSlot : public SlotBase {
  Point2D _minAnchor{};
  Point2D _maxAnchor{};
  Point2D _alignment{};
  Rect _rect;
  bool _sizeToContent;
public:
  PanelSlot(const Managed<Widget>& widget);
  void SetMinAnchor(const Point2D& anchor);
  void SetMaxAnchor(const Point2D& anchor);
  void SetAlignment(const Point2D& alignment);
  void SetRect(const Rect& rect);
  void SetSizeToContent(bool val);

  Point2D GetMinAnchor() const;
  Point2D GetMaxAnchor() const;
  Point2D GetAlignment() const;
  Rect GetRect() const;
  bool GetSizeToContent() const;
};

class Panel : public TMultiSlotWidget<PanelSlot> {
public:
  void Init(WidgetSubsystem *outer) override;
  void BeforeDestroy() override;
  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  Rect ComputeSlotRect(const Managed<PanelSlot> &child) const;

  std::optional<uint32_t> GetMaxSlots() const override;
};
}
