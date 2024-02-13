#pragma once
#include "types.hpp"
#include "vengine/Managed.hpp"

namespace vengine::widget {
class Widget;

template <class SlotType>
class ISlot {
public:

  virtual Ref<SlotType> AddChild(const Managed<Widget>& widget) = 0;

  virtual bool RemoveChild(const Managed<Widget>& widget) = 0;

  virtual Ref<SlotType> GetChildSlot(size_t index) = 0;

  virtual Array<Ref<SlotType>> GetSlots() const = 0;

  virtual std::optional<uint32_t> GetMaxSlots() const = 0;
};
}
