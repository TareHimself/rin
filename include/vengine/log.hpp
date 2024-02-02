#pragma once
#include <spdlog/spdlog.h>

namespace vengine::log {
const auto engine = spdlog::default_logger()->clone("engine");
const auto io = spdlog::default_logger()->clone("io");
const auto physics = spdlog::default_logger()->clone("physics");
const auto input = spdlog::default_logger()->clone("input");
}
