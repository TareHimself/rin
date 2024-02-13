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
  Anchor _xAnchor;
  Anchor _yAnchor;
  Rect _rect;
  bool _sizeToContent;
public:
  PanelSlot(const Managed<Widget>& widget);
  void SetAnchorX(const Anchor& anchor);
  void SetAnchorY(const Anchor& anchor);
  void SetRect(const Rect& rect);
  void SetSizeToContent(bool val);
  Anchor GetAnchorX() const;
  Anchor GetAnchorY() const;
  Rect GetRect() const;
  bool GetSizeToContent() const;
};

class Panel : public TMultiSlotWidget<PanelSlot> {
public:
  void Init(WidgetSubsystem *outer) override;
  void BeforeDestroy() override;
  void Draw(drawing::SimpleFrameData *frameData, DrawInfo info) override;

  Rect ComputeSlotRect(const Managed<PanelSlot> &child);

  std::optional<uint32_t> GetMaxSlots() const override;
};
}
