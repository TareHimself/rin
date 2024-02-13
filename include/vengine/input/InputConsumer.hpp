#pragma once
#include "AxisInputEvent.hpp"
#include "InputSubsystem.hpp"
#include "vengine/Object.hpp"
#include "vengine/input/KeyInputEvent.hpp"
#include <variant>

namespace vengine::input {
typedef std::function<bool(const std::shared_ptr<KeyInputEvent> &handler)>keyHandler;
typedef std::function<bool(const std::shared_ptr<AxisInputEvent> &handler)>axisHandler;
typedef std::function<void()> unsubscribe;
typedef std::pair<std::optional<keyHandler>, std::optional<keyHandler>> keyHandlerPair;


template <typename T>
struct TInputHandlers {
private:
  std::mutex _mutex;
  std::atomic<uint64_t> _lastId = 0;
  std::map<uint64_t, T> _handlers;

public:
  bool ForEach(const std::function<bool(T)> &item);

  unsubscribe Add(const T &handler);

  TInputHandlers();
};

template <typename T> bool TInputHandlers<T>::ForEach(
    const std::function<bool(T)> &item) {
  for (const auto &handler : _handlers) {
    if (item(handler.second)) {
      return true;
    }
  }

  return false;
}

template <typename T> unsubscribe TInputHandlers<T>::Add(const T &handler) {
  auto id = ++_lastId;
  _handlers.emplace(id, handler);

  return [this,id] {
    if (_handlers.contains(id)) {
      _handlers.erase(_handlers.find(id));
    }
  };
}

template <typename T> TInputHandlers<T>::TInputHandlers() {
  _lastId = 0;
  
}

struct KeyInputHandlers : public TInputHandlers<keyHandlerPair>{
  
};


struct AxisInputHandlers : public TInputHandlers<axisHandler> {
  
};

class InputConsumer : public Object<InputSubsystem> {
  std::shared_ptr<KeyInputHandlers> _keyHandlers = std::make_shared<KeyInputHandlers>();
  std::unordered_map<bindableKey,std::shared_ptr<KeyInputHandlers>> _keySpecificHandlers;

  std::unordered_map<EInputAxis, std::shared_ptr<AxisInputHandlers>> _axisHandlers;

public:
  virtual bool CanConsumeInput();
  virtual bool ReceiveAxis(EInputAxis axis,
                           const std::shared_ptr<AxisInputEvent> &event);
  // virtual bool ReceiveMouseDown(
  //     const window::EMouseButton &button, const std::shared_ptr<window::
  //     MouseButtonEvent> &event);
  // virtual bool ReceiveMouseUp(
  //     const window::EMouseButton &button, const std::shared_ptr<window::
  //     MouseButtonEvent> &event);
  virtual bool ReceiveKeyDown(const bindableKey &key,
                              const std::shared_ptr<KeyInputEvent> &event);
  virtual bool ReceiveKeyUp(const bindableKey &key,
                            const std::shared_ptr<KeyInputEvent> &event);

  unsubscribe BindAxis(EInputAxis axis, const axisHandler &handler);
  unsubscribe BindKey(const std::optional<keyHandler> &down, const std::optional<keyHandler> &up) const;
  unsubscribe BindKey(const bindableKey &key,const std::optional<keyHandler> &down, const std::optional<keyHandler> &up);
  // unsubscribe BindMouse(const mouseHandler &down,const mouseHandler &up) const;
  // unsubscribe BindMouse(const window::EMouseButton &button,
  //                       const mouseHandler &down,const mouseHandler &up);


};
}
