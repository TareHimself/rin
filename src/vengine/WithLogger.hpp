#pragma once
#include <string>
#include <spdlog/spdlog.h>

namespace vengine {
class WithLogger {
  std::shared_ptr<spdlog::logger> _logger;
public:
  void InitLogger(const std::string &name);
  std::shared_ptr<spdlog::logger> GetLogger() const;
};
}

