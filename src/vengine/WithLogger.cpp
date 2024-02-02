#include <vengine/WithLogger.hpp>
namespace vengine {
void WithLogger::InitLogger(const std::string &name) {
  _logger = spdlog::default_logger()->clone(name);
  _logger->info("Logger Created");
}

std::shared_ptr<spdlog::logger> WithLogger::GetLogger() const {
  return _logger;
}
}

