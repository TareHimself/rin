#pragma once
#include <functional>
#include <ranges>
#include <future>
#include <filesystem>
#include <vector>

template <typename E, typename T, typename Iter>
std::vector<E> mapRange(const Iter& begin, const Iter& end, const std::function<E(const T&)>& transform);

template <typename E, typename T, typename Iter>
std::vector<E> mapRange(const Iter& begin, const Iter& end, const std::function<E(const T&)>& transform)
{
    std::vector<E> result{};
    for (auto it = begin; it != end; ++it)
    {
        result.push_back(transform(*it));
    }
    return result;
}

template <typename Base, typename T>
bool instanceOf(const T* ptr);

template <typename Base, typename T>
bool instanceOf(const T* ptr)
{
    return dynamic_cast<const Base*>(ptr) != nullptr;
}

std::vector<unsigned char> readFile(const std::filesystem::path& path);


std::string readFileAsString(const std::filesystem::path& path);

template <typename T>
std::shared_future<T> sharedFutureFromResult(const T& result);

template <typename T>
std::shared_future<T> sharedFutureFromResult(const T& result)
{
    return std::shared_future<T>(std::async(std::launch::deferred, [](const T& arg) { return arg; }, result));
}

std::filesystem::path getResourcesPath();


// Computes a compile time bitshift using template arguments. if no arguments are passed returns the bitshift for 0
uint32_t bitshift();

// Computes a compile time bitshift using template arguments. if no arguments are passed returns the bitshift for 0
//std::iterator_traits<InputIt>::value_type
// Computes a compile time bitshift using template arguments. if no arguments are passed returns the bitshift for 0
template <typename T = uint32_t, typename... Bits, typename = std::enable_if_t<std::is_integral_v<T> &&
              std::conjunction_v<std::is_integral<Bits>...>>>
T bitshift(Bits... bits);

template <typename Iterator, typename = std::enable_if_t<std::is_integral_v<typename std::iterator_traits<
              Iterator>::value_type>>>
typename std::iterator_traits<Iterator>::value_type bitmaskForRange(Iterator begin, Iterator end);

template <typename T, typename... Bits, typename>
T bitshift(Bits... bits)
{
    T result = 0;

    ((result |= (1 << bits)), ...);
    return result;
}

template <typename Iterator, typename>
typename std::iterator_traits<Iterator>::value_type bitmaskForRange(Iterator begin, Iterator end)
{
    typename std::iterator_traits<Iterator>::value_type result = 0;
    for (auto it = begin; it != end; ++it)
    {
        auto bit = *it;
        result |= 1 << bit;
    }
    if (result == 0)
    {
        result = 0xFF;
    }
    return result;
}
