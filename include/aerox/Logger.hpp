#pragma once

#include "typedefs.hpp"
#include "containers/TDelegate.hpp"
#include <fmt/format.h>
#include <iostream>

namespace aerox {
typedef std::function<void(const std::string &)> transportFn;

class Logger {
  std::string _name;

public:
  DECLARE_DELEGATE(onLog, const std::string&)
  DECLARE_DELEGATE(onInfo, const std::string&)
  DECLARE_DELEGATE(onWarn, const std::string&)
  DECLARE_DELEGATE(onError, const std::string&)

  explicit Logger(const std::string &name);

  template <typename... T>
  std::string Format(const std::string &category,
                     const fmt::format_string<T...> &message, T &&... args);

  template <typename... T>
  void Log(const fmt::format_string<T...> &message, T &&... args);

  template <typename... T>
  void Log(const std::string &category, const fmt::format_string<T...> &message,
           T &&... args);

  template <typename... T>
  void Info(const fmt::format_string<T...> &message, T &&... args);

  template <typename... T>
  void Warn(const fmt::format_string<T...> &message, T &&... args);

  template <typename... T>
  void Error(const fmt::format_string<T...> &message, T &&... args);

  void Log(const std::string &message);

  void Log(const std::string &category, const std::string &message);

  void Info(const std::string &message);

  void Warn(const std::string &message);

  void Error(const std::string &message);
};

inline Logger::Logger(const std::string &name) {
  _name = name;
}


template <typename... T>
std::string Logger::Format(
    const std::string &category, const fmt::format_string<T...> &message,
    T &&... args) {
  if (category.empty()) {
    return fmt::format("[{}] :: {}", _name,
                       fmt::format(message, std::forward<T>(args)...));
  }
  return fmt::format("[{}] :: [{}] :: {}", category, _name,
                     fmt::format(message, std::forward<T>(args)...));
}

template <typename... T>
void Logger::Log(
    const fmt::format_string<T...> &message, T &&... args) {
  Log(fmt::format(message, std::forward<T>(args)...));
}

template <typename... T>
void Logger::Log(const std::string &category,
                 const fmt::format_string<T...> &message, T &&... args) {
  Log(category, fmt::format(message, std::forward<T>(args)...));
}

template <typename... T>
void Logger::Info(
    const fmt::format_string<T...> &message, T &&... args) {
  Info(fmt::format(message, std::forward<T>(args)...));
}

template <typename... T>
void Logger::Warn(
    const fmt::format_string<T...> &message, T &&... args) {
  Warn(fmt::format(message, std::forward<T>(args)...));
}

template <typename... T>
void Logger::Error(
    const fmt::format_string<T...> &message, T &&... args) {
  Error(fmt::format(message, std::forward<T>(args)...));
}

inline void Logger::Log(const std::string &message) {
  onLog->Execute(Format("", "{}", message));
}

inline void Logger::Log(const std::string &category,
                        const std::string &message) {
  onLog->Execute(Format(category, "{}", message));
}

inline void Logger::Info(const std::string &message) {
  onInfo->Execute(Format("info", "{}", message));
}

inline void Logger::Warn(const std::string &message) {
  onWarn->Execute(Format("warn", "{}", message));
}

inline void Logger::Error(const std::string &message) {
  onError->Execute(Format("error", "{}", message));
}


class ConsoleLogger : public Logger {
public:
  explicit ConsoleLogger(const std::string &name);
};

inline ConsoleLogger::ConsoleLogger(const std::string &name)
  : Logger(name) {
  onLog->BindFunction([](const std::string &msg) {
    std::cout << msg << std::endl;
    //fmt::println("{}",msg);
  });

  onInfo->BindFunction([](const std::string &msg) {
    std::cout << msg << std::endl;
    //fmt::println("{}",msg);
  });

  onWarn->BindFunction([](const std::string &msg) {
    std::cout << msg << std::endl;
    //fmt::println("{}",msg);
  });

  onError->BindFunction([](const std::string &msg) {
    std::cerr << msg << std::endl;
    //fmt::println("{}",msg);
  });
}
}
