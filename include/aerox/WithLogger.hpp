#pragma once
#include "Logger.hpp"
#include <string>

namespace aerox {
class WithLogger {
  std::shared_ptr<ConsoleLogger> _logger;
public:
  void InitLogger(const std::string &name);
  std::shared_ptr<ConsoleLogger> GetLogger() const;
};
}

