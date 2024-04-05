#pragma once

#include <cmath>
#include <stdexcept>
#include <optional>
#include <functional>


template<typename T, typename ...TExtraArgs>
using TFindFunc = std::function<bool(T &, TExtraArgs...)>;

class IndexOutOfRangeError : public std::runtime_error {
    [[nodiscard]] const char *what() const override {
        return "Index Out Of Range";
    }
};

template<typename T>
class TContainer {
public:

    virtual bool Add(const T &data) = 0;

    virtual bool Add(T &&data) = 0;

    template<typename ...TArgs, typename = std::enable_if_t<((std::is_convertible_v<TArgs, T>) && ...)>>
    size_t AddAll(TArgs... data);

    virtual size_t Remove(size_t index, size_t count = 1) = 0;

    virtual void Reserve(const size_t &size) = 0;

    [[nodiscard]] virtual size_t Size() const = 0;

    virtual bool Empty() const = 0;

    virtual std::optional<size_t> IndexOf(T &&data) const = 0;

    virtual T &operator[](const size_t &index) = 0;

    virtual T &operator[](const size_t &index) const = 0;

    virtual T &Get(const size_t &idx) const = 0;

    virtual T *Find(const TFindFunc<T> &callback) const = 0;

    virtual T *Find(const TFindFunc<T, size_t> &callback) const = 0;

    virtual std::optional<size_t> FindIndex(const TFindFunc<T> &callback) const = 0;

    virtual std::optional<size_t> FindIndex(const TFindFunc<T, size_t> &callback) const = 0;
};

template<typename T>
template<typename ... TArgs, typename>
size_t TContainer<T>::AddAll(TArgs... data) {
    size_t items = 0;

    ([&] {
        if (Add(data)) {
            items++;
        }
    }, ...);

    return items;
}

