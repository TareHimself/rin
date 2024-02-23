#pragma once
#include "ISlot.hpp"
#include "Widget.hpp"

namespace vengine::widget {

template<typename T>
class TMultiSlotWidget : public Widget, public ISlot<T> {

protected:
  Array<Managed<T>> _slots;
public:
  Ref<T> Add(const Managed<Widget> &widget) override;
  bool RemoveChild(const Managed<Widget> &widget) override;
  Ref<T> GetChildSlot(size_t index) override;
  Array<Ref<T>> GetSlots() const override;
  std::optional<uint32_t> GetMaxSlots() const override = 0;
  virtual void NotifyAddedToScreen() override;
  virtual void NotifyRemovedFromScreen() override;
  virtual void NotifyRootChanged(WidgetRoot *root) override;
};

template <typename T> Ref<T> TMultiSlotWidget<T>::Add(const Managed<Widget> &widget) {
  if(_slots.size() + 1 > GetMaxSlots().value_or(std::numeric_limits<size_t>::max())) {
    return {};
  }
  
  widget->SetParent(this);
  
  auto slot = _slots.emplace_back(Managed(new T(widget)));
  
  _children.emplace_back(widget);
  
  if(IsOnScreen()) {
    widget->NotifyAddedToScreen();
  }
  
  InvalidateCachedSize();
  return slot;
}

template <typename T> bool TMultiSlotWidget<T>::RemoveChild(const Managed<Widget> &widget) {
  for(auto i = 0; i < _children.size(); i++) {
    if(_slots[i]->GetWidget() == widget) {
      
      _slots[i]->GetWidget().Reserve()->SetParent(nullptr);
      
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

template <typename T> Ref<T> TMultiSlotWidget<T>::GetChildSlot(size_t index) {
  if(index < _slots.size()) {
    return _slots[index];
  }

  return {};
}

template <typename T> Array<Ref<T>> TMultiSlotWidget<T>::GetSlots() const {
  return _slots.template map<Ref<T>>([](size_t _,const Managed<T>& item) {
    return item;
  });
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
    WidgetRoot *root) {
  Widget::NotifyRootChanged(root);
  for(auto &child : _children) {
    child->NotifyRootChanged(root);
  }
}

}

