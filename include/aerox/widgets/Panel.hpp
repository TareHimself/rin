#pragma once
#include "ISlot.hpp"
#include "SlotBase.hpp"
#include "TMultiSlotWidget.hpp"
#include "Widget.hpp"
#include "units.hpp"

namespace aerox::widgets {

struct Anchor {
  float min;
  float max;
};

typedef std::pair<std::shared_ptr<Unit>,std::shared_ptr<Unit>> panelSlotSize;
class PanelSlot : public SlotBase {
  Point2D _minAnchor{};
  Point2D _maxAnchor{};
  Point2D _alignment{};
  Point2D _point;
  panelSlotSize _size = {0.0_u,0.0_u};;
  bool _sizeToContent;
  
public:
  PanelSlot(const std::shared_ptr<Widget>& widget);
  void SetMinAnchor(const Point2D& anchor);
  void SetMaxAnchor(const Point2D& anchor);
  void SetAlignment(const Point2D& alignment);
  void SetPoint(const Point2D& point);
  void SetSize(const panelSlotSize& size);
  void SetSizeToContent(bool val);
  std::optional<Rect> computed;

  Point2D GetMinAnchor() const;
  Point2D GetMaxAnchor() const;
  Point2D GetAlignment() const;
  Point2D GetPoint() const;
  std::pair<std::shared_ptr<Unit>, std::shared_ptr<Unit>> GetSize() const;
  bool GetSizeToContent() const;
};

class Panel : public TMultiSlotWidget<PanelSlot> {
public:

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  Rect ComputeSlotRect(const std::shared_ptr<PanelSlot> &child) const;

  std::optional<uint32_t> GetMaxSlots() const override;
};
}
