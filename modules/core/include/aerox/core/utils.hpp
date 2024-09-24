#pragma once
#include <functional>
#include <ranges>
#include <future>
#include <filesystem>
#include <vector>

template<typename E,typename T,typename Iter>
    std::vector<E> mapRange(const Iter& begin,const Iter& end,const std::function<E(const T&)>& transform);

template <typename E, typename T, typename Iter>
std::vector<E> mapRange(const Iter& begin, const Iter& end, const std::function<E(const T&)>& transform)
{
    std::vector<E> result{};
    for (auto it = begin; it != end; ++it) {
        result.push_back(transform(*it));
    }
    return result;
}

template<typename Base, typename T>
bool instanceOf(const T *ptr);
    
template<typename Base, typename T>
bool instanceOf(const T *ptr) {
    return dynamic_cast<const Base*>(ptr) != nullptr;
}

std::string readFileAsString(const std::filesystem::path& path);

template<typename T>
std::shared_future<T> sharedFutureFromResult(const T& result);

template <typename T>
std::shared_future<T> sharedFutureFromResult(const T& result)
{
    return std::shared_future<T>(std::async(std::launch::deferred,[](const T& arg){ return arg; },result));
}
    
std::filesystem::path getResourcesPath();
