#pragma once
#include "TArray.hpp"
#include <set>

template<typename T>
class TSet : public TArray<T> {
    std::set<T> _set;
public:

    TSet() = default;

    ~TSet() = default;

    bool Add(const T &data) override {

    }

    bool Add(T &&data) override {

    }

    bool Remove(T &&data){
        auto r = _set.erase(data);
        return true;
    }

    bool Remove(const T &data){
        auto r = _set.erase(data);
        return true;
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

    virtual bool Contains(const T& item) const{

    }


};
