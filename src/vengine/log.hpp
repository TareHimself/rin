#pragma once
#include <spdlog/spdlog.h>

namespace vengine {
namespace log {
const auto engine = spdlog::default_logger()->clone("engine");
const auto io = spdlog::default_logger()->clone("io");
const auto physics = spdlog::default_logger()->clone("physics");
const auto rendering = spdlog::default_logger()->clone("rendering");
const auto shaders = spdlog::default_logger()->clone("shaders");
}
}
