#include <aerox/WithLogger.hpp>
namespace aerox {
void WithLogger::InitLogger(const std::string &name) {
  _logger = std::make_shared<ConsoleLogger>(name);
}

std::shared_ptr<ConsoleLogger> WithLogger::GetLogger() const {
  return _logger;
}
}

