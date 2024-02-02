#ifndef VENGINE_TYPES
#define VENGINE_TYPES
#include <deque>
#include <functional>
#include <map>
#include <ranges>

namespace vengine {
typedef std::function<void()> cleanupCallback;
typedef std::shared_ptr<std::optional<uint64_t>> volatileId;

struct CleanupQueue {
private:
  std::map<uint64_t,std::function<void()>> _cleaners;
  std::atomic<uint64_t> lastCleanerId = std::numeric_limits<uint64_t>::max();
public:
  uint64_t Push(const std::function<void()> & cleaner) {
    auto id = --lastCleanerId;
    _cleaners.emplace(id,cleaner);
    return id;
  }

  void Remove(const uint64_t id) {
    if(_cleaners.contains(id)) {
      _cleaners.erase(_cleaners.find(id));
    }
  }

  void Run() {
    for(const auto &val : _cleaners | std::views::values) {
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
