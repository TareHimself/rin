#pragma once

#include "TContainer.hpp"
#include <cmath>
#include <stdexcept>
#include <optional>
#include <functional>


template<typename T>
class TArray : public TContainer<T> {

    T *_data = nullptr;
    size_t _size = 0;
    size_t _allocatedSize = 0;

protected:
    void CheckIncrement() {
        if (_size + 1 >= _allocatedSize) {
            if (_allocatedSize == 0) {
                Reserve(20);
            } else {
                Reserve(std::max(_allocatedSize * 1.3f, 20.0f));
            }
        }
    }

public:

    TArray() {
        _data = nullptr;
    }

    ~TArray() {
        delete[] _data;
    }

    bool Add(const T &data) override {
        CheckIncrement();
        _data[_size] = data;
        _size++;
    }

    bool Add(T &&data) override {
        CheckIncrement();
        _data[_size] = data;
        _size++;
    }

    size_t Remove(size_t index, size_t count) override {
        if (index >= Size()) {
            throw std::runtime_error("Index out of range");
        }

        auto start = index;
        auto stop = std::min(index + count, Size());
        if (start == stop) {
            return 0;
        }

        auto diff = stop - start;

        std::copy_n(_data + stop, diff, _data + start);

        _size -= diff;

        return diff;
    }

    void Reserve(const size_t &size) override {
        if (_data == nullptr) {
            _data = new T[size];
        } else {
            auto newData = new T[size];

            std::copy_n(_data, _size, newData);

            delete[] _data;

            _data = newData;
        }

        _allocatedSize = size;
    }

    size_t Size() const override {
        return _size;
    }

    bool Empty() const override {
        return _size == 0;
    }

    std::optional<size_t> IndexOf(T &&data) const override {
        for (auto i = 0; i < Size(); i++) {
            if (this->Get(i) == data) {
                return i;
            }
        }

        return {};
    }

    T &operator[](const size_t &index) override {
        if(index >= Size()){
            throw std::runtime_error("Index out of range");
        }

        return _data[index];
    }

    T &operator[](const size_t &index) const override {
        if (index >= Size()) {
            throw std::runtime_error("Index out of range");
        }

        return _data[index];
    }

    T &Get(const size_t &idx) const override {
        if (idx >= Size()) {
            throw std::runtime_error("Index out of range");
        }

        return _data[idx];
    }

    T *Find(const TFindFunc<T> &callback) const override {
        for (auto i = 0; i < Size(); i++) {
            if (callback(this->Get(i))) {
                return &this->Get(i);
            }
        }

        return nullptr;
    }

    T *Find(const TFindFunc<T, size_t> &callback) const override {
        for (auto i = 0; i < Size(); i++) {
            if (callback(this->Get(i), i)) {
                return &this->Get(i);
            }
        }

        return nullptr;
    }

    std::optional<size_t> FindIndex(const TFindFunc<T> &callback) const override {
        for (auto i = 0; i < Size(); i++) {
            if (callback(this->Get(i))) {
                return i;
            }
        }

        return {};
    }

    std::optional<size_t> FindIndex(const TFindFunc<T, size_t> &callback) const override {
        for (auto i = 0; i < Size(); i++) {
            if (callback(this->Get(i), i)) {
                return i;
            }
        }

        return {};
    }



//    template<typename E>
//    TArray<E> Map(const std::function<E(T&)>& callback);
//
//    template<typename E>
//    TArray<E> Map(const std::function<E(T&,size_t)>& callback);
};
