#pragma once
#include "TMultiSlotWidget.hpp"


namespace aerox::widgets {

template<typename T>
class Scrollable : public TMultiSlotWidget<T>{
  float _scroll = 0.0f;
public:
  virtual std::optional<uint32_t> GetMaxSlots() const override;
  
  virtual float GetMaxScroll() const = 0;
  virtual bool IsScrollable() const = 0;

  virtual float GetScrollOffset() const;
  virtual float GetScroll() const;
  virtual bool ScrollTo(float position);
  virtual bool ScrollBy(float delta);
  virtual void ClampScroll();
};

template <typename T> std::optional<uint32_t> Scrollable<T>::GetMaxSlots() const {
  return {};
}

template <typename T> float Scrollable<T>::GetScroll() const {
  return _scroll;
}

template <typename T> float Scrollable<T>::GetScrollOffset() const {
  return GetScroll() * -1.0f;
}

template <typename T> bool Scrollable<T>::ScrollTo(float position) {
  if(!IsScrollable()) {
    return false;
  }

  const auto lastPosition = GetScroll();
  
  _scroll = std::clamp(position,0.0f,GetMaxScroll());

  return lastPosition != GetScroll();
}

template <typename T> bool Scrollable<T>::ScrollBy(float delta) {
  return ScrollTo(_scroll + delta);
}

template <typename T> void Scrollable<T>::ClampScroll() {
  _scroll = std::clamp(GetScroll(),0.0f,GetMaxScroll());
}


}
