#pragma once
#include <spdlog/spdlog.h>

namespace vengine {
namespace log {
const auto engine = spdlog::default_logger()->clone("engine");
const auto io = spdlog::default_logger()->clone("io");
const auto physics = spdlog::default_logger()->clone("physics");
const auto drawing = spdlog::default_logger()->clone("drawing");
const auto shaders = spdlog::default_logger()->clone("shaders");
const auto assets = spdlog::default_logger()->clone("assets");
}
}
