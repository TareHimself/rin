#ifndef VENGINE_TYPES
#define VENGINE_TYPES
#include <atomic>
#include <functional>
#include <map>
#include <ranges>

namespace vengine {
typedef std::function<void()> cleanupCallback;

template <typename Result, typename... Args>
struct TFunction {
private:
  uint64_t _id = 0;
  std::function<Result(Args...)> _func;

public:
  TFunction(uint64_t id, const std::function<Result(Args...)> &func);
  bool operator==(const TFunction &other) const;
  bool operator<(const TFunction &other) const;
  bool operator>(const TFunction &other) const;
  uint64_t GetId() const;
  std::function<Result(Args...)> GetFunction();
  
  Result operator ()(Args... args) const;
};

template <typename Result, typename... Args> TFunction<
  Result, Args...>::TFunction(const uint64_t id,
                              const std::function<Result(Args...)> &func) {
  _id = id;
  _func = func;
}

template <typename Result, typename... Args> bool TFunction<
  Result, Args...>::operator==(const TFunction &other) const {
  return _id == other._id;
}

template <typename Result, typename... Args> bool TFunction<
  Result, Args...>::operator<(const TFunction &other) const {
  return _id < other._id;
}

template <typename Result, typename... Args> bool TFunction<
  Result, Args...>::operator>(const TFunction &other) const {
  return _id > other._id;
}

template <typename Result, typename... Args> uint64_t TFunction<
  Result, Args...>::GetId() const {
  return _id;
}

template <typename Result, typename... Args> std::function<Result(Args...)>
TFunction<Result, Args...>::GetFunction() {
  return _func;
}

template <typename Result, typename ... Args> Result TFunction<Result, Args...>::operator()(Args... args) const {
  return _func(std::forward<Args>(args)...);
}

struct CleanupQueue {
private:
  std::map<uint64_t, std::function<void()>> _cleaners;
  std::atomic<uint64_t> lastCleanerId = std::numeric_limits<uint64_t>::max();

public:
  uint64_t Push(const std::function<void()> &cleaner) {
    auto id = --lastCleanerId;
    _cleaners.emplace(id, cleaner);
    return id;
  }

  void Remove(const uint64_t id) {
    if (_cleaners.contains(id)) {
      _cleaners.erase(_cleaners.find(id));
    }
  }

  void Run() {
    for (const auto &val : _cleaners | std::views::values) {
      val();
    }

    _cleaners.clear();

    lastCleanerId = 0;
  }
};

enum EInputMode {
  GameOnly,
  UiOnly,
  GameAndUi
};
}
#endif
