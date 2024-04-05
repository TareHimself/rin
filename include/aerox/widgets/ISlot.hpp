#pragma once
#include "types.hpp"
#include "aerox/typedefs.hpp"

namespace aerox::widgets {
class Widget;

template <class SlotType>
class ISlot {
public:

  virtual std::weak_ptr<SlotType> AddChild(const std::shared_ptr<Widget>& widget) = 0;

  virtual bool RemoveChild(const std::shared_ptr<Widget>& widget) = 0;

  virtual std::weak_ptr<SlotType> GetChildSlot(size_t index) = 0;

  virtual Array<std::weak_ptr<SlotType>> GetSlots() const = 0;

  virtual std::optional<uint32_t> GetMaxSlots() const = 0;
};
}
