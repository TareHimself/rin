#pragma once
#include <spdlog/spdlog.h>

namespace vengine::log {
const auto engine = spdlog::default_logger()->clone("engine");
const auto io = spdlog::default_logger()->clone("io");
const auto physics = spdlog::default_logger()->clone("physics");
const auto drawing = spdlog::default_logger()->clone("drawing");
const auto shaders = spdlog::default_logger()->clone("shaders");
const auto assets = spdlog::default_logger()->clone("assets");
const auto input = spdlog::default_logger()->clone("input");
const auto scripting = spdlog::default_logger()->clone("scripting");
}
