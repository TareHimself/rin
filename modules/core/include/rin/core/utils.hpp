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


// Computes a compile time bitmask using template arguments. if no arguments are passed returns the bitmask for 0
uint32_t bitmask();

// Computes a compile time bitmask using template arguments. if no arguments are passed returns the bitmask for 0
//std::iterator_traits<InputIt>::value_type
// Computes a compile time bitmask using template arguments. if no arguments are passed returns the bitmask for 0
template<typename T = uint32_t,typename ...Bits,typename = std::enable_if_t<std::is_integral_v<T> && std::conjunction_v<std::is_integral<Bits>...>>>
T bitmask(Bits... bits);

template<typename Iterator,typename = std::enable_if_t<std::is_integral_v<std::iterator_traits<Iterator>::value_type>>>
std::iterator_traits<Iterator>::value_type bitmask(Iterator begin,Iterator end);

template <typename T, typename ... Bits, typename>
T bitmask(Bits... bits)
{
    T result{};
    ((bits == 0 ? result |= 0x0 : result |= (1 << (bits - 1))), ...);
    return result;
}

template <typename Iterator, typename>
typename std::iterator_traits<Iterator>::value_type bitmask(Iterator begin, Iterator end)
{
    std::iterator_traits<Iterator>::value_type result{};
    for(auto it = begin; it != end; ++it)
    {
        auto bit = *it;
        result |= bit == 0 ? 0x0 : (1 << (bit - 1));
    }
    return result;
}
