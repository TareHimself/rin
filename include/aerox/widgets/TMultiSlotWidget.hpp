#pragma once
#include "ISlot.hpp"
#include "Widget.hpp"

namespace aerox::widgets {
class SlotBase;

template<typename T = SlotBase>
class TMultiSlotWidget : public Widget, public ISlot<T> {

protected:
  Array<std::shared_ptr<T>> _slots;

  void NotifyAddedToScreen() override;
  void NotifyRemovedFromScreen() override;
  void NotifyRootChanged(const std::weak_ptr<WidgetRoot>& root) override;
  
public:
  std::weak_ptr<T> AddChild(const std::shared_ptr<Widget> &widget) override;
  bool RemoveChild(const std::shared_ptr<Widget> &widget) override;
  std::weak_ptr<T> GetChildSlot(size_t index) override;
  Array<std::weak_ptr<T>> GetSlots() const override;
  [[nodiscard]] std::optional<uint32_t> GetMaxSlots() const override = 0;
  uint32_t GetNumOccupiedSlots() const;
  void Tick(float deltaTime) override;
};

template <typename T> std::weak_ptr<T> TMultiSlotWidget<T>::AddChild(const std::shared_ptr<Widget> &widget) {
  if(_slots.size() + 1 > GetMaxSlots().value_or(std::numeric_limits<size_t>::max())) {
    return {};
  }
  
  widget->SetParent(utils::cast<Widget>(this->shared_from_this()));
  
  auto slot = _slots.emplace_back(std::make_shared<T>(widget));
  
  _children.emplace_back(widget);
  
  if(IsOnScreen()) {
    widget->NotifyAddedToScreen();
  }
  
  InvalidateCachedSize();
  return slot;
}

template <typename T> bool TMultiSlotWidget<T>::RemoveChild(const std::shared_ptr<Widget> &widget) {
  for(auto i = 0; i < _children.size(); i++) {
    if(const std::shared_ptr<Widget> slotWidget = _slots[i]->GetWidget().lock()) {
      slotWidget->SetParent({});
      if(IsOnScreen()) {
        _children[i]->NotifyRemovedFromScreen();
      }

      _slots.remove(i);
      
      _children.remove(i);
      
      InvalidateCachedSize();
      return true;
    }
  }

  return false;
}

template <typename T> std::weak_ptr<T> TMultiSlotWidget<T>::GetChildSlot(size_t index) {
  if(index < _slots.size()) {
    return _slots[index];
  }

  return {};
}

template <typename T> Array<std::weak_ptr<T>> TMultiSlotWidget<T>::GetSlots() const {
  return _slots.template map<std::weak_ptr<T>>([](size_t _,const std::shared_ptr<T>& item) {
    return item;
  });
}

template <typename T> uint32_t TMultiSlotWidget<T>::GetNumOccupiedSlots() const {
  return _slots.size();
}

template <typename T> void TMultiSlotWidget<T>::Tick(float deltaTime) {
  Widget::Tick(deltaTime);
  for(auto &child : _children) {
    child->Tick(deltaTime);
  }
}

template <typename T> void TMultiSlotWidget<T>::NotifyAddedToScreen() {
  Widget::NotifyAddedToScreen();
  for(auto &child : _children) {
    child->NotifyAddedToScreen();
  }
}

template <typename T> void TMultiSlotWidget<T>::NotifyRemovedFromScreen() {
  Widget::NotifyRemovedFromScreen();
  for(auto &child : _children) {
      child->NotifyRemovedFromScreen();
    }
}

template <typename T> void TMultiSlotWidget<T>::NotifyRootChanged(
    const std::weak_ptr<WidgetRoot> &root) {
  Widget::NotifyRootChanged(root);
  for(auto &child : _children) {
    child->NotifyRootChanged(root);
  }
}

}

