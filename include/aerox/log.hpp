#pragma once
#include "Logger.hpp"
#include <memory>

namespace aerox::log {
const auto engine = std::make_shared<ConsoleLogger>("Engine");
const auto io = std::make_shared<ConsoleLogger>("Io");
const auto physics = std::make_shared<ConsoleLogger>("Physics");
const auto input = std::make_shared<ConsoleLogger>("Input");
const auto meta = std::make_shared<ConsoleLogger>("meta");
}
