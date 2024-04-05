#pragma once
namespace aerox {
#ifdef _WIN32
#define VENGINE_PLATFORM_WIN
#endif

#ifdef __APPLE__
#define VENGINE_PLATFORM_MAC
#endif

#ifdef __linux__
#define VENGINE_PLATFORM_LINUX
#endif

}
