#pragma once
#include <unordered_map>

template <typename TItem, typename TInternalKey, typename TPublicKey>
class Store
{
    std::unordered_map<TInternalKey, TItem> _items{};

public:
    virtual ~Store() = default;

    virtual TItem CreateNew(const TPublicKey& key) = 0;
    virtual TInternalKey GetInternalKey(const TPublicKey& key) = 0;
    virtual TItem Create(const TPublicKey& key);
    std::unordered_map<TInternalKey, TItem> GetItems() const;
};

template <typename TItem, typename TInternalKey, typename TPublicKey>
TItem Store<TItem, TInternalKey, TPublicKey>::Create(const TPublicKey& key)
{
    auto internalKey = GetInternalKey(key);

    if (_items.contains(internalKey)) return _items.at(internalKey);

    return _items.emplace(internalKey, CreateNew(key)).first->second;
}

template <typename TItem, typename TInternalKey, typename TPublicKey>
std::unordered_map<TInternalKey, TItem> Store<TItem, TInternalKey, TPublicKey>::GetItems() const
{
    return _items;
}
